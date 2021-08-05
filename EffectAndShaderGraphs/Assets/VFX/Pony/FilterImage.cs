using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class FilterImage : MonoBehaviour
{
    [SerializeField]
    private VisualEffect _visualEffect;
    // Start is called before the first frame update

    [SerializeField]
    private Texture2D _texture2D;

    [SerializeField]
    private float treshholdToFilterPixels;

    [SerializeField]
    private float treshholdToMakePixelBlack;

    private const int filterSize = 8;
    private readonly Color[] _filteringPixels = new Color[filterSize];

    void Start()
    {
        FilterTexture();
        _visualEffect.SetTexture("Texture", _texture2D);
        _visualEffect.SendEvent("OnPlayCustom");
    }

    private void FilterTexture()
    {
        for (int i = 0; i < _texture2D.width; i++)
        {
            for (int j = 0; j < _texture2D.height; j++)
            {
                FilterPixel(i, j);
            }
        }

        _texture2D.Apply();
    }

    private void FilterPixel(int m, int k)
    {
        int pixelsWithSmallTreshhold = 0;
        var centerPixel = _texture2D.GetPixel(m, k);

        if (centerPixel.r < treshholdToMakePixelBlack)
        {
            _texture2D.SetPixel(m, k, Color.black);
        }
        else
        {
            int count = 0;

            for (int i = m - 1; i <= m + 1; i++)
            {
                for (int j = k - 1; j <= k + 1; j++)
                {
                    if (i != m || j != k)
                    {
                        if (i >= 0 && i < _texture2D.width && j >= 0 && j < _texture2D.height)
                        {
                            var pixel = _texture2D.GetPixel(i, j);

                            if (Mathf.Abs(pixel.r - centerPixel.r) < treshholdToFilterPixels)
                            {
                                pixelsWithSmallTreshhold++;
                            }

                            _filteringPixels[count] = pixel;
                            count++;
                        }
                    }
                }
            }

            if (pixelsWithSmallTreshhold < 4)
            {
                var averagePixelValue = Color.black;

                for (int i = 0; i < count; i++)
                {
                    averagePixelValue.r += _filteringPixels[i].r;
                }

                if (count > 0)
                {
                    averagePixelValue.r /= count;
                }

                if (averagePixelValue.r < treshholdToMakePixelBlack)
                {
                    averagePixelValue.r = 0;
                }

                _texture2D.SetPixel(m, k, averagePixelValue);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}