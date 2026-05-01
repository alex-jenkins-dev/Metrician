// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Rendering
{
    /// <summary>
    /// Orthographic orbit camera. The ortho slab's visible height is derived from
    /// <see cref="FieldOfView"/> and <see cref="Distance"/> so zoom feel matches
    /// a perspective camera at the focal plane.
    /// </summary>
    public class Camera3D
    {
        public Vector3 Target { get; set; } = Vector3.Zero;
        public float Azimuth { get; set; } = 0f;
        public float Elevation { get; set; } = 0.5f;
        public float Distance { get; set; } = 10f;

        /// <summary>Vertical FOV in degrees. Slab height = 2 * Distance * tan(FOV / 2).</summary>
        public float FieldOfView { get; set; } = 45f;

        /// <summary>Slab extends symmetrically from -FarPlane to +FarPlane along the view direction.</summary>
        public float FarPlane { get; set; } = 10_000f;

        public float MinDistance { get; set; } = 0.01f;
        public float MaxDistance { get; set; } = 100_000f;

        public float MinElevation { get; set; } = -MathF.PI / 2f;
        public float MaxElevation { get; set; } = MathF.PI / 2f;

        /// <summary>
        /// World-space camera position. Z-up convention: at Az=0, El=0 the eye
        /// sits at -Y so Front looks toward +Y with +X right and +Z up.
        /// </summary>
        public Vector3 Eye
        {
            get
            {
                float cosEl = MathF.Cos(Elevation);
                float sinEl = MathF.Sin(Elevation);
                float sinAz = MathF.Sin(Azimuth);
                float cosAz = MathF.Cos(Azimuth);
                return Target + Distance * new Vector3(
                     cosEl * sinAz,
                    -cosEl * cosAz,
                     sinEl);
            }
        }

        /// <summary>
        /// World-space up, recomputed from the orbit so it never flips. Up is
        /// the eye direction shifted in elevation by π/2; the identities
        /// cos(El + π/2) = -sin(El), sin(El + π/2) = cos(El) give the form below.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                float cosEl = MathF.Cos(Elevation);
                float sinEl = MathF.Sin(Elevation);
                float sinAz = MathF.Sin(Azimuth);
                float cosAz = MathF.Cos(Azimuth);
                return new Vector3(
                    -sinEl * sinAz,
                     sinEl * cosAz,
                     cosEl);
            }
        }

        public Matrix4x4 ViewMatrix =>
            Matrix4x4.CreateLookAt(Eye, Target, Up);

        public Matrix4x4 ProjectionMatrix(int viewportWidth, int viewportHeight)
        {
            float aspect = viewportHeight == 0
                ? 1f
                : (float)viewportWidth / viewportHeight;

            float orthoHeight = 2f * Distance *
                                MathF.Tan(FieldOfView * MathF.PI / 180f * 0.5f);
            float orthoWidth = aspect * orthoHeight;
            return Matrix4x4.CreateOrthographic(
                orthoWidth, orthoHeight, -FarPlane, FarPlane);
        }

        public Matrix4x4 ViewProjection(int viewportWidth, int viewportHeight) =>
            ViewMatrix * ProjectionMatrix(viewportWidth, viewportHeight);

        public void Orbit(float deltaAzimuth, float deltaElevation)
        {
            Azimuth = (Azimuth + deltaAzimuth) % (2f * MathF.PI);
            Elevation = Math.Clamp(Elevation + deltaElevation, MinElevation, MaxElevation);
        }

        /// <summary>
        /// Orbits the whole camera frame about an arbitrary world-space pivot.
        /// Both Eye and Target rotate rigidly around <paramref name="pivot"/>, so
        /// the eye-to-target offset (Distance and look direction) is preserved
        /// and any prior pan stays intact.
        /// </summary>
        public void OrbitAround(Vector3 pivot, float deltaAzimuth, float deltaElevation)
        {
            float oldEl = Elevation;
            float newEl = Math.Clamp(oldEl + deltaElevation, MinElevation, MaxElevation);
            float clampedDeltaEl = newEl - oldEl;

            var oldEye = Eye;
            var oldTarget = Target;

            var azRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, deltaAzimuth);

            // Camera-right axis after the azimuth rotation. atan2-style fallback
            // when the view is straight up/down so cross(viewDir, Z) doesn't vanish.
            var view = oldEye - oldTarget;
            var oldRight = Vector3.Cross(view, Vector3.UnitZ);
            if (oldRight.LengthSquared() < 1e-12f)
                oldRight = Vector3.UnitX;
            oldRight = Vector3.Normalize(oldRight);
            var newRight = Vector3.Transform(oldRight, azRot);

            var elRot = Quaternion.CreateFromAxisAngle(newRight, clampedDeltaEl);
            var rotation = elRot * azRot;

            Target = pivot + Vector3.Transform(oldTarget - pivot, rotation);
            Azimuth = (Azimuth + deltaAzimuth) % (2f * MathF.PI);
            Elevation = newEl;
        }

        /// <summary>Scales Distance by <paramref name="factor"/>; &gt;1 zooms out, &lt;1 zooms in.</summary>
        public void Zoom(float factor)
        {
            Distance = Math.Clamp(Distance * factor, MinDistance, MaxDistance);
        }

        /// <summary>Zooms toward <paramref name="worldPoint"/>, keeping it fixed under the cursor.</summary>
        public void ZoomToward(Vector3 worldPoint, float factor)
        {
            float oldDistance = Distance;
            Zoom(factor);
            float actualFactor = oldDistance > 0f ? Distance / oldDistance : factor;
            Target = worldPoint + actualFactor * (Target - worldPoint);
        }

        /// <summary>Pans Target in the camera's local right/up plane (world units).</summary>
        public void Pan(float deltaX, float deltaY)
        {
            Matrix4x4 view = ViewMatrix;
            var right = new Vector3(view.M11, view.M12, view.M13);
            var up = new Vector3(view.M21, view.M22, view.M23);
            Target -= right * deltaX + up * deltaY;
        }

        public void Reset()
        {
            Target = Vector3.Zero;
            Azimuth = 0f;
            Elevation = 0.5f;
            Distance = 10f;
        }

        public void SetView(StandardView view)
        {
            switch (view)
            {
                case StandardView.Front: Azimuth = 0f; Elevation = 0f; break;
                case StandardView.Back: Azimuth = MathF.PI; Elevation = 0f; break;
                case StandardView.Right: Azimuth = MathF.PI / 2f; Elevation = 0f; break;
                case StandardView.Left: Azimuth = -MathF.PI / 2f; Elevation = 0f; break;
                case StandardView.Top: Azimuth = 0f; Elevation = MathF.PI / 2f; break;
                case StandardView.Bottom: Azimuth = 0f; Elevation = -MathF.PI / 2f; break;
                case StandardView.Isometric:
                    // 45 deg azimuth + atan(1/sqrt(2)) ~= 35.26 deg elevation:
                    // the canonical isometric view where the three axes project
                    // at 120 deg to one another.
                    // https://en.wikipedia.org/wiki/Isometric_projection
                    Azimuth = MathF.PI / 4f;
                    Elevation = MathF.Atan(1f / MathF.Sqrt(2f));
                    break;
            }
        }

        /// <summary>Unprojects a screen point to a world-space ray for picking.</summary>
        public Ray3D ScreenToRay(PointF screen, int vpWidth, int vpHeight)
        {
            Matrix4x4.Invert(ViewProjection(vpWidth, vpHeight), out Matrix4x4 invVP);
            return ScreenToRayFromInvVP(invVP, screen, vpWidth, vpHeight);
        }

        /// <summary>
        /// Screen-space ray from a pre-computed inverse VP. Lets a caller freeze
        /// the camera frame mid-drag so the anchor does not drift as Target moves.
        /// Unproject two NDC points along the depth axis and take their difference;
        /// see https://www.khronos.org/opengl/wiki/GluProject_and_gluUnProject_code
        /// </summary>
        public static Ray3D ScreenToRayFromInvVP(
            Matrix4x4 invVP, PointF screen, int vpWidth, int vpHeight)
        {
            float ndcX = (screen.X / vpWidth) * 2f - 1f;
            float ndcY = -((screen.Y / vpHeight) * 2f - 1f);

            var near = Vector4.Transform(new Vector4(ndcX, ndcY, 0f, 1f), invVP);
            var far  = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), invVP);

            var origin = new Vector3(near.X, near.Y, near.Z);
            var direction = Vector3.Normalize(
                new Vector3(far.X - near.X, far.Y - near.Y, far.Z - near.Z));

            return new Ray3D(origin, direction);
        }
    }
}
