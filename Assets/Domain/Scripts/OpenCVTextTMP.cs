using System;
using System.Text;

using UnityEngine;

using TMPro;
using System.Linq.Expressions;

[Serializable]
public class ArgumetsOpenCVTextTMP
{
    public bool writeVersion = false;
    public bool writeTickFrequency = false;
}

public class OpenCVTextTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text _textTMP;
    [SerializeField] private ArgumetsOpenCVTextTMP _argumetsWriteTextTMP;

    private bool textIsChaged = false;

    private void OnEnable()
    {
        var stringBuilder = new StringBuilder(_textTMP.text);

        void ChangeTextTMP(Action lambda)
        {
            try
            {
                if (textIsChaged) lambda();
                else
                {
                    textIsChaged = true;

                    lambda();
                }
            }
            catch(Exception e)
            {
                _textTMP.text = $"{_textTMP.text} {e.Message}";
            }
        }

        if (_argumetsWriteTextTMP.writeVersion) ChangeTextTMP(() => stringBuilder.Append($"Версия OpenCV: {OpenCVBridge.GetVersion()}"));

        if (_argumetsWriteTextTMP.writeTickFrequency) ChangeTextTMP(() => stringBuilder.Append($"TickFrequency: {OpenCVBridge.GetTickFrequency()}"));

        if (textIsChaged) _textTMP.text = stringBuilder.ToString(); 
    }

    public void Close() => gameObject.SetActive(false);
}
