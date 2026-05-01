// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class IntersectionNodeTemplate : INodeTemplate
    {
        // |dot(plane.normal, cylinder.axis)| above this threshold counts as perpendicular.
        private const float PerpendicularityTolerance = 0.999f;

        public string Title => "Intersection";
        public string Vendor => "Geometry";
        public string Description =>
            "Intersects a plane with a cylinder and outputs the resulting circle. " +
            "Succeeds only when the plane is perpendicular to the cylinder's axis " +
            "and the plane crosses the cylinder within its finite height. " +
            "Otherwise the node enters the error state.";

        public void Configure(INodeAuthor a)
        {
            var planePin = a.Pins.AddInput<PlaneSpec>("plane");
            var cylinderPin = a.Pins.AddInput<CylinderSpec>("cylinder");
            a.Pins.AddOutput<CircleSpec>("circle");

            a.Pins.Constrain(planePin.Id, c => c ? null : "plane must be wired");
            a.Pins.Constrain(cylinderPin.Id, c => c ? null : "cylinder must be wired");

            a.Behaviour.OnEvaluate(ctx =>
            {
                var plane = ctx.Read<PlaneSpec>("plane");
                var cylinder = ctx.Read<CylinderSpec>("cylinder");
                if (plane is null || cylinder is null) return;

                var normal = plane.Normal;
                var axis = cylinder.Axis;
                if (normal.LengthSquared() < 1e-12f || axis.LengthSquared() < 1e-12f)
                {
                    ctx.Error("plane normal and cylinder axis must be non-zero");
                    return;
                }
                normal = Vector3.Normalize(normal);
                axis = Vector3.Normalize(axis);

                float alignment = MathF.Abs(Vector3.Dot(normal, axis));
                if (alignment < PerpendicularityTolerance)
                {
                    ctx.Error("plane is not perpendicular to the cylinder's axis");
                    return;
                }

                // Signed distance from the cylinder centre, along the axis,
                // to where the plane meets the axis.
                float t = Vector3.Dot(plane.Center - cylinder.Center, axis);
                if (MathF.Abs(t) > cylinder.Height * 0.5f)
                {
                    ctx.Error("plane does not intersect the cylinder");
                    return;
                }

                ctx.Write("circle", new CircleSpec(
                    cylinder.Center + axis * t,
                    axis,
                    cylinder.Radius,
                    plane.Colour));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("intersection");
        }
    }
}
