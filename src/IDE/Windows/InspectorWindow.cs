using System;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using MonoGameMaker.IDE.Core;

namespace MonoGameMaker.IDE.Windows
{
    public static class InspectorWindow
    {
        public static void Draw()
        {
            ImGui.Begin("Inspector");

            if (GlobalState.SelectedNode == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), "No entity selected.");
                ImGui.End();
                return;
            }

            var node = GlobalState.SelectedNode;
            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), $"Entity: {node.prefabName}");
            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 5));

            // Tarefa 3: Bloqueio de Edição no Play Mode
            if (GlobalState.IsPlaying)
            {
                ImGui.BeginDisabled();
            }

            ImGui.Text("Node Properties:");
            float x = node.x;
            float y = node.y;
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("X##NodeX", ref x)) node.x = x;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("Y##NodeY", ref y)) node.y = y;

            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 5));
            ImGui.Text("Components:");

            foreach (var comp in node.Components)
            {
                string headerName = comp.GetType().Name;
                if (ImGui.CollapsingHeader(headerName, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawComponentFields(comp);
                }
            }

            if (GlobalState.IsPlaying)
            {
                ImGui.EndDisabled();
            }

            ImGui.End();
        }

        private static void DrawComponentFields(Component comp)
        {
            Type type = comp.GetType();
            
            // Get public fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                object? val = field.GetValue(comp);
                if (val == null) continue;

                ImGui.Text(field.Name);
                ImGui.SameLine(120);

                if (field.FieldType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##F_{field.Name}", ref intVal))
                    {
                        field.SetValue(comp, intVal);
                    }
                }
                else if (field.FieldType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##F_{field.Name}", ref floatVal))
                    {
                        field.SetValue(comp, floatVal);
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##F_{field.Name}", ref boolVal))
                    {
                        field.SetValue(comp, boolVal);
                    }
                }
                else if (field.FieldType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##F_{field.Name}", ref strVal, 256))
                    {
                        field.SetValue(comp, strVal);
                    }
                }
                else if (field.FieldType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new System.Numerics.Vector2(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##F_{field.Name}", ref numVec))
                    {
                        field.SetValue(comp, new Vector2(numVec.X, numVec.Y));
                    }
                }
                else if (field.FieldType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new System.Numerics.Vector4(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##F_{field.Name}", ref numCol))
                    {
                        field.SetValue(comp, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                    }
                }
            }

            // Get public properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.Name == "Parent" || prop.Name == "Enabled" || prop.Name == "Time" || prop.Name == "Input") continue;

                object? val = prop.GetValue(comp);
                if (val == null) continue;

                ImGui.Text(prop.Name);
                ImGui.SameLine(120);

                if (prop.PropertyType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##P_{prop.Name}", ref intVal))
                    {
                        prop.SetValue(comp, intVal);
                    }
                }
                else if (prop.PropertyType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##P_{prop.Name}", ref floatVal))
                    {
                        prop.SetValue(comp, floatVal);
                    }
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##P_{prop.Name}", ref boolVal))
                    {
                        prop.SetValue(comp, boolVal);
                    }
                }
                else if (prop.PropertyType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##P_{prop.Name}", ref strVal, 256))
                    {
                        prop.SetValue(comp, strVal);
                    }
                }
                else if (prop.PropertyType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new System.Numerics.Vector2(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##P_{prop.Name}", ref numVec))
                    {
                        prop.SetValue(comp, new Vector2(numVec.X, numVec.Y));
                    }
                }
                else if (prop.PropertyType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new System.Numerics.Vector4(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##P_{prop.Name}", ref numCol))
                    {
                        prop.SetValue(comp, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                    }
                }
            }
        }
    }
}
