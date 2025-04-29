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

using UnityEditor.ShaderGraph;

namespace ShaderGraphExtensions
{
    class DebugValueShaderUtils
    {
        // Modified version of https://www.shadertoy.com/view/4sBSWW
        // Smaller Number Printing - @P_Malin
        // Creative Commons CC0 1.0 Universal (CC-0)
        
        public static string DebugValueFunctionName = "SGE_DebugValue";
        public static void DebugValueFunction(FunctionRegistry registry)
        {
            registry.ProvideFunction("SGE_DebugValue_ExtractBit", s => s.Append(@"
inline float SGE_DebugValue_ExtractBit(float n, float b)
{
    return fmod(floor(n / exp2(floor(b))), 2.0f);
}"));
            
            registry.ProvideFunction("SGE_DebugValue_ExtractDecimal", s => s.Append(@"
inline float SGE_DebugValue_ExtractDecimal(float n, float index)
{
    return fmod(n / pow(10.0f, index), 10.0f);
}"));
            
            registry.ProvideFunction("SGE_DebugValue_DigitToBin", s => s.Append(@"
inline float SGE_DebugValue_DigitToBin(int x)
{
    return  x == 0 ? 0x69996 : 
            x == 1 ? 0x62227 : 
            x == 2 ? 0xE168F : 
            x == 3 ? 0xE161E : 
            x == 4 ? 0x99711 : 
            x == 5 ? 0xF8E1E :
            x == 6 ? 0x68E96 : 
            x == 7 ? 0xF1244 :
            x == 8 ? 0x69696 : 
            x == 9 ? 0x69716 : 
            x == 10 ? 0x00700 : // 10 is minus sign
            x == 11 ? 0x00004 : 0xFFFFF; // 11 is dot sign
}"));
            
            registry.ProvideFunction("SGE_DebugValue_DrawDigit", s => s.Append(@"
inline float SGE_DebugValue_DrawDigit(int n, float2 uv)
{
    uv = floor(uv);
    int i = SGE_DebugValue_DigitToBin(n);
    return SGE_DebugValue_ExtractBit(float(i), fmod(uv.y, 5.0f) * 4.0f + 3.0f - uv.x);
}"));
            
            registry.ProvideFunction("SGE_DebugValue_DrawFloat", s => s.Append(@"
inline float SGE_DebugValue_DrawFloat(float val, int numberOfDecimalDigit, float2 uv)
{
    float n = floor(uv.x / 5.0f);
    uv.x -= n * 5.0f;
    
    // check for out of digid bounds - bounds are hardcoded as x = 4, y = 5
    if (uv.x < 0.0f || uv.x > 4.0f) 
        return 0.0f;
    if (uv.y < 0.0f || uv.y > 5.0f)
        return 0.0f;
    if (n < 0.0f || n > 8.0f) 
        return 0.0f;

    if (sign(val) < 0.0f)
    {
        if (n == 0.0f) 
            return SGE_DebugValue_DrawDigit(10, uv); // minus sign
        n -= 1.0f; 
        val = abs(val);
    }
    
    float intCount = floor(val) == 0.0f ? 1.0f : floor(log(val) / 2.302585f) + 1.0f;
    float count = intCount + numberOfDecimalDigit;
    
    val *= pow(10.0f, numberOfDecimalDigit);
    
    if (intCount <= n)
    {
        if(intCount == n) 
            return SGE_DebugValue_DrawDigit(11, uv); // dot sign
        n -= 1.0f;
    }
    
    if (count <= n)
        return 0.0f;
    
    return SGE_DebugValue_DrawDigit(int(SGE_DebugValue_ExtractDecimal(val, count - n - 1.0f)), uv);
}"));
            
            registry.ProvideFunction(DebugValueFunctionName, s => s.Append(@"
inline void SGE_DebugValue(float4 DebugValueToDisplay, float2 Uv, float2 TextPos, float TextScale, float ySpacing, out float4 Out)
{
    // color could be exposed if necessary
    float3 xColor = float3(1, 0.3, 0.3);
    float3 yColor = float3(0.3, 1, 0.3);
    float3 zColor = float3(0.3, 0.3, 1);
    float3 wColor = float3(1, 1, 1);

    float2 uvOffset = Uv - TextPos;
    float textScaling = TextScale / 100.0f;
    
    float2 rescaledUvX = uvOffset / textScaling;
    float2 rescaledUvY = (uvOffset + float2(0.0f, ySpacing)) / textScaling;
    float2 rescaledUvZ = (uvOffset + float2(0.0f, 2.0f * ySpacing)) / textScaling;
    float2 rescaledUvW = (uvOffset + float2(0.0f, 3.0f * ySpacing)) / textScaling;
    
    const int NumberOfDecimalDigit = 3; // don't expose that to parameters, it introduces artefacts (like displaying 10.2 in 10.199)
    
    float floatX = SGE_DebugValue_DrawFloat(DebugValueToDisplay.x, NumberOfDecimalDigit, rescaledUvX);
    float floatY = SGE_DebugValue_DrawFloat(DebugValueToDisplay.y, NumberOfDecimalDigit, rescaledUvY);
    float floatZ = SGE_DebugValue_DrawFloat(DebugValueToDisplay.z, NumberOfDecimalDigit, rescaledUvZ);
    float floatW = SGE_DebugValue_DrawFloat(DebugValueToDisplay.w, NumberOfDecimalDigit, rescaledUvW);
    
    float3 color = lerp(float3(0.0f, 0.0f, 0.0f), xColor, floatX);
    color = lerp(color, yColor, floatY);
    color = lerp(color, zColor, floatZ);
    color = lerp(color, wColor, floatW);
       
    Out = float4(color, 1.0);
}"));
        }
    }
}
