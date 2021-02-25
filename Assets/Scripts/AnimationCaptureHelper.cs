using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCaptureHelper : MonoBehaviour
{
    [SerializeField]
    private GameObject _target = null;

    [SerializeField]
    private AnimationClip _sourceClip = null;

    [SerializeField]
    private int _framesPerSecond = 30;

    [SerializeField]
    private Vector2Int _cellSize = new Vector2Int(100, 100);

    [SerializeField]
    private Camera _captureCamera = null;

    [SerializeField]
    private int _cantPixelsClean = 0;
    private int _totalPixelClean = 0;

    private List<Vector2> vec = new List<Vector2>();

    private int _currentFrame = 0;


    public IEnumerator CaptureAnimation(Action<Texture2D, Texture2D> onComplete)
    {

        int numFrames = (int)(_sourceClip.length * _framesPerSecond);
        int gridCellCount = Mathf.CeilToInt(Mathf.Sqrt(numFrames));
        Vector2Int atlasSize = new Vector2Int(_cellSize.x * gridCellCount, _cellSize.y * gridCellCount);
        Vector2Int atlasPos = new Vector2Int(0, atlasSize.y - _cellSize.y);

        Texture2D diffuseMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        ClearAtlas(diffuseMap, Color.clear);

        Texture2D normalMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        ClearAtlas(normalMap, new Color(0.5f, 0.5f, 1.0f, 0.0f));

        RenderTexture rtFrame = new RenderTexture(_cellSize.x, _cellSize.y, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point,
            antiAliasing = 1,
        };

        Shader normalCaptureShader = Shader.Find("Hidden/ViewSpaceNormal");

        _captureCamera.targetTexture = rtFrame;
        Color cachedCameraColor = _captureCamera.backgroundColor;


        for (_currentFrame = 0; _currentFrame < numFrames; _currentFrame++)
        {
            float currentTime = (_currentFrame / (float)numFrames) * _sourceClip.length;
            _sourceClip.SampleAnimation(_target, currentTime);
            yield return null;

            _captureCamera.backgroundColor = Color.clear;
            _captureCamera.Render();
            Graphics.SetRenderTarget(rtFrame);
            diffuseMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            diffuseMap.Apply();

            _captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
            _captureCamera.RenderWithShader(normalCaptureShader, "");
            Graphics.SetRenderTarget(rtFrame);
            normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            normalMap.Apply();

            atlasPos.x += _cellSize.x;
            if ((_currentFrame + 1) % gridCellCount == 0)
            {

                atlasPos.x = 0;
                atlasPos.y -= _cellSize.y;
            }

        }
        CleanAltas(diffuseMap);
        //CleanAltas(normalMap);

        onComplete.Invoke(diffuseMap, normalMap);

        Graphics.SetRenderTarget(null);
        _captureCamera.targetTexture = null;
        _captureCamera.backgroundColor = cachedCameraColor;
        DestroyImmediate(rtFrame);

    }

    // Sets all the pixels in the texture to a specified color.
    private void ClearAtlas(Texture2D texture, Color color)
    {
        Color[] pixels = new Color[texture.width * texture.height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void CleanAltas(Texture2D texture)
    {
        if (_cantPixelsClean > 0)
        {
            Color colorA = new Color(0, 0, 0, 0);
            Color pixelColor = new Color(0, 0, 0, 0);
            Color[] memColor = new Color[_cantPixelsClean];
            //bool allCl=true;

            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    pixelColor = texture.GetPixel(i, j);
                    if (pixelColor.a != colorA.a)
                    {
                        if (CheckAllNearPixels(texture, pixelColor, i, j, _cantPixelsClean) || _totalPixelClean <= _cantPixelsClean)
                        {
                            texture.SetPixel(i, j, colorA);
                            texture.Apply();
                        }
                    }
                    _totalPixelClean = 0;
                    vec.Clear();

                }
            }

        }
    }

    private bool CheckAllNearPixels(Texture2D texture, Color colorPixel, int x, int y, int cantCleans)
    {
        bool alone = true;
        Color pixelColor = colorPixel;
        Color colorA = new Color(0, 0, 0, 0);
        vec.Add(new Vector2(x , y));
        //↖
        if (x - 1 >= 0 && y - 1 >= 0)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x - 1 || vec[i].y != y - 1)
                {
                    pixelColor = texture.GetPixel(x - 1, y - 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x - 1, y - 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //⬆
        if (x - 1 >= 0 && y >= 0)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x - 1 || vec[i].y != y)
                {
                    pixelColor = texture.GetPixel(x - 1, y);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x - 1, y, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //↗
        if (x - 1 >= 0 && y + 1 <= texture.width)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x - 1 || vec[i].y != y + 1)
                {
                    pixelColor = texture.GetPixel(x - 1, y + 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x - 1, y + 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //⬅
        if (y - 1 >= 0)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x || vec[i].y != y - 1)
                {
                    pixelColor = texture.GetPixel(x, y - 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x, y - 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //➡
        if (y + 1 <= texture.width)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x || vec[i].y != y + 1)
                {
                    pixelColor = texture.GetPixel(x, y + 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x, y + 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //↙
        if (x + 1 <= texture.height && y - 1 >= 0)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x + 1 || vec[i].y != y - 1)
                {
                    pixelColor = texture.GetPixel(x + 1, y - 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x + 1, y - 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }
        //⬇
        if (x + 1 <= texture.height)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x + 1 || vec[i].y != y)
                {
                    pixelColor = texture.GetPixel(x + 1, y);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x + 1, y, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                    }
                }
                else
                    alone = false;
            }
        }
        //↘
        if (x + 1 <= texture.height && y + 1 <= texture.width)
        {
            for (int i = 0; i < vec.Count; i++)
            {
                if (vec[i].x != x + 1 || vec[i].y != y + 1)
                {
                    pixelColor = texture.GetPixel(x + 1, y + 1);
                    if (pixelColor.a != colorA.a)
                    {
                        if (cantCleans >= 0)
                        {
                            cantCleans--;
                            if (!CheckAllNearPixels(texture, pixelColor, x + 1, y + 1, cantCleans))
                            {
                                alone = false;
                                _totalPixelClean++;
                            }
                        }
                        else
                            alone = false;
                    }
                }
            }
        }

        return alone;
    }
}
