// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Presentation.Graph
{
    public sealed class GraphTheme
    {
        public Color Background     { get; init; } = Color.FromArgb(30, 30, 35);

        public Color NodeBackground { get; init; } = Color.FromArgb(45, 45, 48);
        public Color NodeBorder     { get; init; } = Color.FromArgb(63, 63, 70);
        public Color NodeHeader     { get; init; } = Color.FromArgb(60, 60, 66);
        public Color SelectedBorder { get; init; } = Color.FromArgb(0, 122, 204);
        public Color Text           { get; init; } = Color.FromArgb(220, 220, 220);
        public Color FooterText     { get; init; } = Color.FromArgb(140, 140, 145);

        public Color Pin              { get; init; } = Color.FromArgb(170, 170, 175);
        public Color PinConnected     { get; init; } = Color.FromArgb(100, 200, 255);
        public Color Wire             { get; init; } = Color.FromArgb(200, 130, 200, 255);
        public Color WireDrag         { get; init; } = Color.FromArgb(220, 255, 200, 100);
        public Color WireError        { get; init; } = Color.FromArgb(220, 220, 80, 80);
        public Color DynamicIndicator { get; init; } = Color.FromArgb(170, 120, 220);

        public Color StatusReady    { get; init; } = Color.FromArgb(120, 220, 120);
        public Color StatusNotReady { get; init; } = Color.FromArgb(230, 170, 70);
        public Color StatusError    { get; init; } = Color.FromArgb(220, 80, 80);

        public Color MenuBackground   { get; init; } = Color.FromArgb(45, 45, 48);
        public Color MenuHover        { get; init; } = Color.FromArgb(62, 62, 64);
        public Color MenuBorder       { get; init; } = Color.FromArgb(63, 63, 70);
        public Color MenuText         { get; init; } = Color.FromArgb(220, 220, 220);
        public Color MenuDisabledText { get; init; } = Color.FromArgb(120, 120, 120);
        public Color MenuArrow        { get; init; } = Color.FromArgb(200, 200, 200);

        public string FontFamily    { get; init; } = "Segoe UI";

        public static GraphTheme Dark { get; } = new();
    }
}
