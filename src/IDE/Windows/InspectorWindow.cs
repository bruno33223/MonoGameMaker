using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Windows
{
    public static class InspectorWindow
    {
        private static float _startDragX;
        private static float _startDragY;
        private static string? _activeCPKey;
        private static string? _startCPValue;

        /// <summary>
        /// Call this inside an existing ImGui tab bar (after BeginTabItem check).
        /// It does NOT call Begin/End — it renders as a tab item inline.
        /// </summary>
        public static void DrawAsTab(string idSuffix)
        {
            if (!ImGui.BeginTabItem($"Inspector##{idSuffix}")) return;

            var selectionContext = GlobalState.SelectionContext;
            if (selectionContext.SelectedNode == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "Click an entity in the Hierarchy or Viewport to inspect it.");
                ImGui.EndTabItem();
                return;
            }

            var node = selectionContext.SelectedNode;
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
                if (ImGui.DragFloat($"X##{idSuffix}_nx", ref x, 1f))
                {
                    node.x = x;
                }
                if (ImGui.IsItemActivated())
                {
                    _startDragX = node.x;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var cmd = new ChangePropertyCommand(node, "x", _startDragX, node.x);
                    GlobalState.CommandManager.ExecuteCommand(cmd);
                }

                ImGui.SetNextItemWidth(-1);
                if (ImGui.DragFloat($"Y##{idSuffix}_ny", ref y, 1f))
                {
                    node.y = y;
                }
                if (ImGui.IsItemActivated())
                {
                    _startDragY = node.y;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var cmd = new ChangePropertyCommand(node, "y", _startDragY, node.y);
                    GlobalState.CommandManager.ExecuteCommand(cmd);
                }
            }

            // --- Custom Properties (key-value from JSON) ---
            if (node.CustomProperties.Count > 0)
            {
                if (ImGui.CollapsingHeader("Custom Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var keys = new List<string>(node.CustomProperties.Keys);
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
                        if (ImGui.IsItemActivated())
                        {
                            _activeCPKey = key;
                            _startCPValue = val;
                        }
                        if (ImGui.IsItemDeactivatedAfterEdit() && _activeCPKey == key)
                        {
                            var cmd = new ChangePropertyCommand(node, "CustomProperties", _startCPValue ?? "", node.CustomProperties[key], key);
                            GlobalState.CommandManager.ExecuteCommand(cmd);
                        }
                    }
                }
            }

            // --- Behavior Script details ---
            if (GlobalState.CurrentProjectPath != null)
            {
                string prefabPath = Path.Combine(GlobalState.CurrentProjectPath, "Prefabs", $"{node.prefabName}.prefab");
                MonoGameMaker.Runtime.PrefabData? prefabData = null;
                if (File.Exists(prefabPath))
                {
                    try
                    {
                        string prefabJson = File.ReadAllText(prefabPath);
                        prefabData = System.Text.Json.JsonSerializer.Deserialize<MonoGameMaker.Runtime.PrefabData>(prefabJson);
                    }
                    catch {}
                }

                if (prefabData != null && !string.IsNullOrEmpty(prefabData.ScriptName))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                    ImGui.Separator();
                    ImGui.TextColored(new System.Numerics.Vector4(0.2f, 0.8f, 0.2f, 1f), $"Script: {prefabData.ScriptName}");

                    // If simulation is running, we can inspect active script variables!
                    if (GlobalState.IsPlaying && AssemblyReloader.LoadedAssembly != null)
                    {
                        object? activeScriptInstance = null;
                        Type? entityManagerType = AssemblyReloader.LoadedAssembly.GetType("MonoGameMaker.Runtime.EntityManager");
                        if (entityManagerType != null)
                        {
                            var entitiesField = entityManagerType.GetField("Entities", BindingFlags.Public | BindingFlags.Static);
                            if (entitiesField != null)
                            {
                                var list = (System.Collections.IList?)entitiesField.GetValue(null);
                                if (list != null)
                                {
                                    foreach (var entity in list)
                                    {
                                        if (entity != null)
                                        {
                                            var prefabNameProp = entity.GetType().GetProperty("PrefabName");
                                            string? pName = prefabNameProp?.GetValue(entity) as string;
                                            if (pName == node.prefabName)
                                            {
                                                var scriptProp = entity.GetType().GetProperty("Script");
                                                activeScriptInstance = scriptProp?.GetValue(entity);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (activeScriptInstance != null)
                        {
                            ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                            ImGui.Text("Runtime values:");
                            DrawObjectFields(activeScriptInstance, idSuffix);
                        }
                        else
                        {
                            ImGui.TextDisabled("Script instance not active in simulation.");
                        }
                    }
                }
                else
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0, 6));
                    ImGui.TextDisabled("No behavior script assigned to this prefab.");
                }
            }

            if (GlobalState.IsPlaying)
            {
                ImGui.EndDisabled();
            }

            ImGui.EndTabItem();
        }

        private static void DrawObjectFields(object obj, string idSuffix)
        {
            Type type = obj.GetType();
            string id = $"{type.Name}_{idSuffix}";

            // Public fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                object? val = field.GetValue(obj);
                if (val == null) continue;

                ImGui.Text(field.Name);
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(-1);

                if (field.FieldType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##F_{id}_{field.Name}", ref intVal))
                        field.SetValue(obj, intVal);
                }
                else if (field.FieldType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##F_{id}_{field.Name}", ref floatVal, 0.1f))
                        field.SetValue(obj, floatVal);
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##F_{id}_{field.Name}", ref boolVal))
                        field.SetValue(obj, boolVal);
                }
                else if (field.FieldType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##F_{id}_{field.Name}", ref strVal, 256))
                        field.SetValue(obj, strVal);
                }
                else if (field.FieldType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##F_{id}_{field.Name}", ref numVec, 0.1f))
                        field.SetValue(obj, new Vector2(numVec.X, numVec.Y));
                }
                else if (field.FieldType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##F_{id}_{field.Name}", ref numCol))
                        field.SetValue(obj, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                }
                else
                {
                    ImGui.TextDisabled($"({field.FieldType.Name})");
                }
            }

            // Public read-write properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                object? val = prop.GetValue(obj);
                if (val == null) continue;

                ImGui.Text(prop.Name);
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(-1);

                if (prop.PropertyType == typeof(int))
                {
                    int intVal = (int)val;
                    if (ImGui.DragInt($"##P_{id}_{prop.Name}", ref intVal))
                        prop.SetValue(obj, intVal);
                }
                else if (prop.PropertyType == typeof(float))
                {
                    float floatVal = (float)val;
                    if (ImGui.DragFloat($"##P_{id}_{prop.Name}", ref floatVal, 0.1f))
                        prop.SetValue(obj, floatVal);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    bool boolVal = (bool)val;
                    if (ImGui.Checkbox($"##P_{id}_{prop.Name}", ref boolVal))
                        prop.SetValue(obj, boolVal);
                }
                else if (prop.PropertyType == typeof(string))
                {
                    string strVal = (string)val;
                    if (ImGui.InputText($"##P_{id}_{prop.Name}", ref strVal, 256))
                        prop.SetValue(obj, strVal);
                }
                else if (prop.PropertyType == typeof(Vector2))
                {
                    Vector2 vecVal = (Vector2)val;
                    System.Numerics.Vector2 numVec = new(vecVal.X, vecVal.Y);
                    if (ImGui.DragFloat2($"##P_{id}_{prop.Name}", ref numVec, 0.1f))
                        prop.SetValue(obj, new Vector2(numVec.X, numVec.Y));
                }
                else if (prop.PropertyType == typeof(Color))
                {
                    Color colVal = (Color)val;
                    System.Numerics.Vector4 numCol = new(colVal.R / 255f, colVal.G / 255f, colVal.B / 255f, colVal.A / 255f);
                    if (ImGui.ColorEdit4($"##P_{id}_{prop.Name}", ref numCol))
                        prop.SetValue(obj, new Color(numCol.X, numCol.Y, numCol.Z, numCol.W));
                }
                else
                {
                    ImGui.TextDisabled($"({prop.PropertyType.Name})");
                }
            }
        }
    }
}
