using System;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using MonoGameMaker.IDE.Core;

namespace MonoGameMaker.IDE.Windows
{
    public static class InspectorWindow
    {
        /// <summary>
        /// Call this inside an existing ImGui tab bar (after BeginTabItem check).
        /// It does NOT call Begin/End — it renders as a tab item inline.
        /// </summary>
        public static void DrawAsTab(string idSuffix)
        {
            if (!ImGui.BeginTabItem($"Inspector##{idSuffix}")) return;

            if (GlobalState.SelectedNode == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "Click an entity in the Hierarchy or Viewport to inspect it.");
                ImGui.EndTabItem();
                return;
            }

            var node = GlobalState.SelectedNode;
            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.85f, 0.85f, 1f), $"{node.prefabName}");
            ImGui.SameLine();
            ImGui.TextDisabled($"({node.x:F0}, {node.y:F0})");
            ImGui.Separator();

            if (GlobalState.IsPlaying)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0.7f, 0.1f, 1f), "[Read-only during simulation]");
                ImGui.BeginDisabled();
            }

            // --- Transform ---
            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                float x = node.x;
                float y = node.y;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.DragFloat($"X##{idSuffix}_nx", ref x, 1f)) node.x = x;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.DragFloat($"Y##{idSuffix}_ny", ref y, 1f)) node.y = y;
            }

            // --- Custom Properties (key-value from JSON) ---
            if (node.CustomProperties.Count > 0)
            {
                if (ImGui.CollapsingHeader("Custom Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var keys = new System.Collections.Generic.List<string>(node.CustomProperties.Keys);
                    foreach (var key in keys)
                    {
                        string val = node.CustomProperties[key];
                        ImGui.Text(key);
                        ImGui.SameLine(120);
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputText($"##CP_{key}_{idSuffix}", ref val, 256))
                        {
                            node.CustomProperties[key] = val;
                        }
                    }
                }
            }

            // --- Attached Components (IDE Component subclasses) ---
            if (node.Components.Count == 0)
            {
                ImGui.Dummy(new System.Numerics.Vector2(0, 6));
                ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "No IDE components attached.");
                ImGui.TextDisabled("Components are C# classes that inherit Component");
                ImGui.TextDisabled("and are listed in the scene JSON 'components' array.");
            }
            else
            {
                foreach (var comp in node.Components)
                {
                    bool compEnabled = comp.Enabled;
                    if (ImGui.Checkbox($"##{comp.GetType().Name}_{idSuffix}_en", ref compEnabled))
                        comp.Enabled = compEnabled;
                    ImGui.SameLine();
                    if (ImGui.CollapsingHeader($"{comp.GetType().Name}##{idSuffix}", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        DrawComponentFields(comp, idSuffix);
                    }
                }
            }

            if (GlobalState.IsPlaying)
            {
                ImGui.EndDisabled();
            }

            ImGui.EndTabItem();
        }

        private static void DrawComponentFields(Component comp, string idSuffix)
        {
            Type type = comp.GetType();
            string id = $"{type.Name}_{idSuffix}";

            // Public fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                object? val = field.GetValue(comp);
                if (val == null) continue;

                ImGui.Text(field.Name);
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(-1);

                if (field.FieldType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##F_{id}_{field.Name}", ref intVal))
                        field.SetValue(comp, intVal);
                }
                else if (field.FieldType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##F_{id}_{field.Name}", ref floatVal, 0.1f))
                        field.SetValue(comp, floatVal);
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##F_{id}_{field.Name}", ref boolVal))
                        field.SetValue(comp, boolVal);
                }
                else if (field.FieldType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##F_{id}_{field.Name}", ref strVal, 256))
                        field.SetValue(comp, strVal);
                }
                else if (field.FieldType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##F_{id}_{field.Name}", ref numVec, 0.1f))
                        field.SetValue(comp, new Vector2(numVec.X, numVec.Y));
                }
                else if (field.FieldType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##F_{id}_{field.Name}", ref numCol))
                        field.SetValue(comp, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                }
                else
                {
                    ImGui.TextDisabled($"({field.FieldType.Name})");
                }
            }

            // Public read-write properties (skip framework internals)
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.Name is "Parent" or "Enabled" or "Time" or "Input") continue;

                object? val = prop.GetValue(comp);
                if (val == null) continue;

                ImGui.Text(prop.Name);
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(-1);

                if (prop.PropertyType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##P_{id}_{prop.Name}", ref intVal))
                        prop.SetValue(comp, intVal);
                }
                else if (prop.PropertyType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##P_{id}_{prop.Name}", ref floatVal, 0.1f))
                        prop.SetValue(comp, floatVal);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##P_{id}_{prop.Name}", ref boolVal))
                        prop.SetValue(comp, boolVal);
                }
                else if (prop.PropertyType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##P_{id}_{prop.Name}", ref strVal, 256))
                        prop.SetValue(comp, strVal);
                }
                else if (prop.PropertyType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##P_{id}_{prop.Name}", ref numVec, 0.1f))
                        prop.SetValue(comp, new Vector2(numVec.X, numVec.Y));
                }
                else if (prop.PropertyType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##P_{id}_{prop.Name}", ref numCol))
                        prop.SetValue(comp, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                }
                else
                {
                    ImGui.TextDisabled($"({prop.PropertyType.Name})");
                }
            }
        }
    }
}
