//
// ShaderGraphExtensions for Unity
// (c) 2020 PH Graphics
// Source code may be used and modified for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset from a pirate site, please consider buying it from the Unity asset store. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace ShaderGraphExtensions
{
    [Title("Utility", "SGE Debug Value")]
    class DebugValue : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        const int DebugValueSlotId = 0;
        const int UvSlotId = 1;
        const int TextPosSlotId = 2;
        const int TextScaleSlotId = 3;
        const int YSpacingSlotId = 4;
        const int OutputSlotId = 5;
        const string kDebugValueSlotName = "DebugValueToDisplay";
        const string kDebugValueSlotDisplayName = "Value To Debug";
        const string kUvSlotName = "Uv";
        const string kTextPosSlotName = "TextPos";
        const string kTextPosSlotDisplayName = "Text Position";
        const string kTextScaleSlotName = "TextScale";
        const string kTextScaleSlotDisplayName = "Text Scale";
        const string kYSpacingSlotName = "YSpacing";
        const string kYSpacingSlotDisplayName = "Y Spacing";
        const string kOutputSlotName = "Out";

        public DebugValue()
        {
            name = "SGE Debug Value";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var idList = new List<int> { DebugValueSlotId, UvSlotId, TextPosSlotId, TextScaleSlotId, YSpacingSlotId, OutputSlotId };
            
            AddSlot(new Vector4MaterialSlot(DebugValueSlotId, kDebugValueSlotDisplayName, kDebugValueSlotName, SlotType.Input, new Vector4(1.0f, 2.0f, 3.0f, 4.0f)));
            AddSlot(new UVMaterialSlot(UvSlotId, kUvSlotName, kUvSlotName, UVChannel.UV0));
            AddSlot(new Vector2MaterialSlot(TextPosSlotId, kTextPosSlotDisplayName, kTextPosSlotName, SlotType.Input, new Vector2(0.1f, 0.8f)));
            AddSlot(new Vector1MaterialSlot(TextScaleSlotId, kTextScaleSlotDisplayName, kTextScaleSlotDisplayName, SlotType.Input, 1.5f));
            AddSlot(new Vector1MaterialSlot(YSpacingSlotId, kYSpacingSlotDisplayName, kYSpacingSlotDisplayName, SlotType.Input, 0.2f));

            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(idList, true);
        }

        private string GetFunctionName()
        {
            return "SGE_DebugValue_Node";
        }

        // generate how a node will be called in code
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string debugValue = GetSlotValue(DebugValueSlotId, generationMode);
            var uvValue = GetSlotValue(UvSlotId, generationMode);
            var textPosValue = GetSlotValue(TextPosSlotId, generationMode);
            var textScaleValue = GetSlotValue(TextScaleSlotId, generationMode);
            var ySpacingValue = GetSlotValue(YSpacingSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6});", GetFunctionName(), debugValue, uvValue, textPosValue, textScaleValue, ySpacingValue, outputValue);
        }

        // generate the node's function
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            string functionName = GetFunctionName();

            DebugValueShaderUtils.DebugValueFunction(registry);

            registry.ProvideFunction(functionName, s =>
            {
                var debugValueSlot = FindInputSlot<MaterialSlot>(DebugValueSlotId);
                var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
                var textPosSlot = FindInputSlot<MaterialSlot>(TextPosSlotId);
                var textScaleSlot = FindInputSlot<MaterialSlot>(TextScaleSlotId);
                var ySpacingSlot = FindInputSlot<MaterialSlot>(YSpacingSlotId);
                var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);

                string test = debugValueSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString());
                Console.WriteLine(test);

                if (uvSlot == null)
                    throw new NullReferenceException("UvSlot null, how is it possible ?");
                if (outputSlot == null)
                    throw new NullReferenceException("outputSlot null, how is it possible ?");

                s.Append("void {0}({1} {2}, {3} {4}, {5} {6}, {7} {8}, {9} {10}, out {11} {12})",
                    functionName,
                    debugValueSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kDebugValueSlotName,
                    uvSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kUvSlotName,
                    textPosSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kTextPosSlotName,
                    textScaleSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kTextScaleSlotName,
                    ySpacingSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kYSpacingSlotName,
                    outputSlot.concreteValueType.ToShaderString(ConcretePrecision.Single.ToShaderString()),
                    kOutputSlotName);

                using (s.BlockScope())
                {
                    s.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6});",
                        DebugValueShaderUtils.DebugValueFunctionName,
                        kDebugValueSlotName,
                        kUvSlotName,
                        kTextPosSlotName,
                        kTextScaleSlotName,
                        kYSpacingSlotName,
                        kOutputSlotName);
                }
            });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }
    }
}