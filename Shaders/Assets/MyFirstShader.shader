Shader "SergShade/HelloShader"{
    
    Properties{
        _myColour ("Example Colour", Color) = (1,1,1,1)
    }

    SubShader{

        CGPROGRAM
            #pragma surface surf Lambert

            struct Input{
                float2 uvMainTax;
            };

            fixed4 _myColour;

            void surf(Input IN, inout SurfaceOutput o){
                o.Albedo = _myColour.rgb;
            }

        ENDCG
    }

    Fallback "Diffuse"
}