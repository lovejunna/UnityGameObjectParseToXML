using System;
using UnityEngine;
using UnityEditor;
using System.Xml;
using System.IO;
using UnityEngine.UI;

public class PrefabParseToXMl : Editor
{
    //将所有游戏场景导出为XML格式
    [MenuItem("Assets/PrefabParseToXMl", false, 1)]
    [MenuItem("GameObject/PrefabParseToXMl", false, 1)]
    [MenuItem("Window/PrefabParseToXMl")]
    private static void ExportXML()
    {
        string filePath = EditorUtility.SaveFilePanel("Save Resource", "", "PrefabParseToXMl", "xml");
        if (!File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        if (filePath.Length != 0)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("e:Skin");
            UnityEngine.Object[] selectedPrefabs = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);
            foreach (var selectPrefab in selectedPrefabs)
            {
                //当关卡启用
                if (selectPrefab != null)
                {
                    root.SetAttribute("class", selectPrefab.name + "Skin");
                    var selectGameObj = selectPrefab as GameObject;
                    var canvasGameObj = xmlDoc.CreateElement("e:Group");
                    var canvasRectTsf = selectGameObj.GetComponent<RectTransform>();
                    canvasGameObj.SetAttribute("name", selectGameObj.name);
                    canvasGameObj.SetAttribute("visible", selectGameObj.gameObject.activeSelf.ToString().ToLower());
                    canvasGameObj.SetAttribute("width", canvasRectTsf.rect.width.ToString());
                    canvasGameObj.SetAttribute("height", canvasRectTsf.rect.height.ToString());
                    canvasGameObj.SetAttribute("x", (canvasRectTsf.GetComponent<RectTransform>().rect.width * (canvasRectTsf.anchorMax.x + canvasRectTsf.anchorMin.x) / 2 + canvasRectTsf.anchoredPosition.x).ToString());
                    canvasGameObj.SetAttribute("y", (canvasRectTsf.GetComponent<RectTransform>().rect.height * (1 - (canvasRectTsf.anchorMax.y + canvasRectTsf.anchorMin.y) / 2) - canvasRectTsf.anchoredPosition.y).ToString());
                    canvasGameObj.SetAttribute("scaleX", canvasRectTsf.localScale.x.ToString());
                    canvasGameObj.SetAttribute("scaleY", canvasRectTsf.localScale.y.ToString());
                    canvasGameObj.SetAttribute("anchorOffsetX", (canvasRectTsf.pivot.x * canvasRectTsf.rect.width).ToString());
                    canvasGameObj.SetAttribute("anchorOffsetY", ((1 - canvasRectTsf.pivot.y) * canvasRectTsf.rect.height).ToString());
                    // Debug.Log($"width:{canvasRectTsf.rect.width},height:{canvasRectTsf.rect.height},x:{ canvasRectTsf.localPosition.x},y: { canvasRectTsf.localPosition.y},scaleX: { canvasRectTsf.localScale.x},scaleY: { canvasRectTsf.localScale.y}");
                    root.AppendChild(canvasGameObj);
                    ParseChildren(xmlDoc, canvasGameObj, selectGameObj.transform);
                    root.SetAttribute("width", Screen.width.ToString());
                    root.SetAttribute("height", Screen.height.ToString());
                    root.SetAttribute("xmlns:e", "http://ns.egret.com/eui");
                    xmlDoc.AppendChild(root);
                    xmlDoc.Save(filePath);
                }
            }
        }
        //刷新Project视图， 不然需要手动刷新哦
        AssetDatabase.Refresh();
        Debug.Log($"已经解析完成!");
    }

    private static void ParseChildren(XmlDocument xmlDoc, XmlElement currentElement, Transform currentGObjTsf)
    {
        UnityComponentParseToH5(xmlDoc, currentElement, currentGObjTsf.GetComponents<Component>());
        for (int i = 0; i < currentGObjTsf.childCount; i++)
        {
            var child = currentGObjTsf.GetChild(i);
            var childGameObj = xmlDoc.CreateElement("e:Group");
            var childRectTsf = child.GetComponent<RectTransform>();
            childGameObj.SetAttribute("name", child.name);
            childGameObj.SetAttribute("visible", child.gameObject.activeSelf.ToString().ToLower());
            childGameObj.SetAttribute("x", (currentGObjTsf.GetComponent<RectTransform>().rect.width * (childRectTsf.anchorMax.x + childRectTsf.anchorMin.x) / 2 + childRectTsf.anchoredPosition.x).ToString());
            childGameObj.SetAttribute("y", (currentGObjTsf.GetComponent<RectTransform>().rect.height * (1 - (childRectTsf.anchorMax.y + childRectTsf.anchorMin.y) / 2) - childRectTsf.anchoredPosition.y).ToString());
            childGameObj.SetAttribute("width", childRectTsf.rect.width.ToString());
            childGameObj.SetAttribute("height", childRectTsf.rect.height.ToString());
            childGameObj.SetAttribute("scaleX", childRectTsf.localScale.x.ToString());
            childGameObj.SetAttribute("scaleY", childRectTsf.localScale.y.ToString());
            childGameObj.SetAttribute("anchorOffsetX", (childRectTsf.pivot.x * childRectTsf.rect.width).ToString());
            childGameObj.SetAttribute("anchorOffsetY", ((1 - childRectTsf.pivot.y) * childRectTsf.rect.height).ToString());
            if (child.childCount > 0)
            {
                ParseChildren(xmlDoc, childGameObj, child);
            }
            else
            {
                var childComponents = child.GetComponents<Component>();
                UnityComponentParseToH5(xmlDoc, childGameObj, childComponents);
            }
            currentElement.AppendChild(childGameObj);
        }
    }


    private static void UnityComponentParseToH5(XmlDocument xmlDoc, XmlElement gameObject, Component[] components)
    {
        foreach (var component in components)
        {
            if (component != null)
            {
                XmlElement componentProps = null;
                //求出最后一个.字符的位置
                var i = component.GetType().ToString().LastIndexOf('.');
                //从指定位置截取字符串
                var componentName = component.GetType().ToString().Substring(i + 1);
                switch ($"{componentName}")
                {
                    case nameof(Image):
                        var image = component as Image;
                        var spritePath = AssetDatabase.GetAssetPath(image.sprite.GetInstanceID());
                        componentProps = xmlDoc.CreateElement("e:Image");
                        if (spritePath != "Resources/unity_builtin_extra" && spritePath.Length != 0)
                        {
                            var spriteSplitPaths = spritePath.Split('/');
                            var spriteName = spriteSplitPaths[spriteSplitPaths.Length - 1].Split('.');
                            string spriteFolderName = null;
                            spriteFolderName = spriteSplitPaths[spriteSplitPaths.Length - 2];
                            var newSpriteName = spriteFolderName[0].ToString().ToLower() + spriteFolderName.Substring(1) + "_" + spriteName[0] + "_" + spriteName[1];
                            componentProps.SetAttribute("source", newSpriteName);
                        }
                        else
                        {
                            componentProps.SetAttribute("source", "white_png");
                        }
                        componentProps.SetAttribute("visible", image.enabled.ToString().ToLower());
                        break;
                    case nameof(Button):
                        var button = component as Button;
                        componentProps = xmlDoc.CreateElement("e:Button");
                        componentProps.SetAttribute("label", "");
                        componentProps.SetAttribute("enabled", button.interactable.ToString().ToLower());
                        componentProps.SetAttribute("visible", button.enabled.ToString().ToLower());
                        break;
                    case nameof(Text):
                        var text = component as Text;
                        var verticalAlign = "";
                        var textAlign = "";
                        componentProps = xmlDoc.CreateElement("e:Label");
                        componentProps.SetAttribute("text", text.text);
                        componentProps.SetAttribute("fontFamily", text.font.ToString().Replace(" (UnityEngine.Font)", ""));
                        componentProps.SetAttribute("textColor", "0x" + ColorUtility.ToHtmlStringRGB(text.color));
                        componentProps.SetAttribute("alpha", text.color.a.ToString());
                        componentProps.SetAttribute("size", text.fontSize.ToString());
                        componentProps.SetAttribute("bold", (text.fontStyle == FontStyle.Bold).ToString().ToLower());
                        componentProps.SetAttribute("italic", (text.fontStyle == FontStyle.Italic).ToString().ToLower());
                        if (text.fontStyle == FontStyle.BoldAndItalic)
                        {
                            componentProps.SetAttribute("bold", "true");
                            componentProps.SetAttribute("italic", "true");
                        }
                        switch (text.alignment)
                        {
                            case TextAnchor.LowerCenter:
                                verticalAlign = "bottom";
                                textAlign = "center";
                                break;
                            case TextAnchor.LowerLeft:
                                verticalAlign = "bottom";
                                textAlign = "left";
                                break;
                            case TextAnchor.LowerRight:
                                verticalAlign = "bottom";
                                textAlign = "right";
                                break;
                            case TextAnchor.MiddleCenter:
                                verticalAlign = "middle";
                                textAlign = "center";
                                break;
                            case TextAnchor.MiddleLeft:
                                verticalAlign = "middle";
                                textAlign = "left";
                                break;
                            case TextAnchor.MiddleRight:
                                verticalAlign = "middle";
                                textAlign = "right";
                                break;
                            case TextAnchor.UpperCenter:
                                verticalAlign = "top";
                                textAlign = "center";
                                break;
                            case TextAnchor.UpperLeft:
                                verticalAlign = "top";
                                textAlign = "left";
                                break;
                            case TextAnchor.UpperRight:
                                verticalAlign = "top";
                                textAlign = "right";
                                break;
                        }
                        componentProps.SetAttribute("verticalAlign", verticalAlign);
                        componentProps.SetAttribute("textAlign", textAlign);
                        componentProps.SetAttribute("lineSpacing", (int)text.lineSpacing + "");
                        componentProps.SetAttribute("visible", text.enabled.ToString().ToLower());
                        break;
                    case nameof(InputField):
                        var inputField = component as InputField;
                        componentProps = xmlDoc.CreateElement("e:EditableText");
                        componentProps.SetAttribute("text", inputField.text);
                        componentProps.SetAttribute("visible", inputField.enabled.ToString().ToLower());
                        break;
                    case nameof(Slider):
                        var slider = component as Slider;
                        componentProps = xmlDoc.CreateElement("e:ProgressBar");
                        componentProps.SetAttribute("enabled", slider.interactable.ToString().ToLower());
                        componentProps.SetAttribute("visible", slider.enabled.ToString().ToLower());
                        break;
                    case nameof(Scrollbar):
                        var scrollbar = component as Scrollbar;
                        componentProps = xmlDoc.CreateElement("e:Scroller");
                        componentProps.SetAttribute("enabled", scrollbar.interactable.ToString().ToLower());
                        componentProps.SetAttribute("visible", scrollbar.enabled.ToString().ToLower());
                        break;
                    case nameof(ScrollRect):
                        var scrollRect = component as ScrollRect;
                        componentProps = xmlDoc.CreateElement("e:List");
                        componentProps.SetAttribute("visible", scrollRect.enabled.ToString().ToLower());
                        break;
                    //我们项目自己写的一个脚本OutlineEx,测试用不了，必须在项目中才能开启
                    //case "OutlineEx":
                    // var outLine = component as OutlineEx;
                    //componentProps = xmlDoc.CreateElement("e:OutLine");
                    // componentProps.SetAttribute("stroke", outLine.OutlineWidth);
                    // componentProps.SetAttribute("strokeColor", outLine.OutlineColor)
                    // componentProps.SetAttribute("visible", outLine.enabled.ToString().ToLower());
                    //break;
                    default:
                        break;
                }
                if (componentProps != null)
                {
                    var rectTransform = component.GetComponent<RectTransform>();
                    componentProps.SetAttribute("width", rectTransform.rect.width.ToString());
                    componentProps.SetAttribute("height", rectTransform.rect.height.ToString());
                    componentProps.SetAttribute("right", 0.ToString());
                    componentProps.SetAttribute("left", 0.ToString());
                    componentProps.SetAttribute("top", 0.ToString());
                    componentProps.SetAttribute("bottom", 0.ToString());
                    gameObject.AppendChild(componentProps);
                }
            }
        }
    }
}