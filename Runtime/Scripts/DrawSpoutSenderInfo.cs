using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Klak.Spout;

namespace IVLab.MinVR3
{



    public class DrawSpoutSenderInfo : MonoBehaviour
    {
        public SpoutSender sender;
        public string label = "Streaming via Spout on ";
        public Color m_TextColor = Color.white;
        public int m_FontSize = 10;
        public TextAnchor m_TextAnchor = TextAnchor.MiddleLeft;
        public Rect m_Position = new Rect(20, 10, 40, 20);

        void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.alignment = m_TextAnchor;
            style.fontSize = m_FontSize;
            style.normal.textColor = m_TextColor;

            string text = label + sender.spoutName;
            Rect pixelPosition = new Rect(m_Position.x * Screen.width, m_Position.y * Screen.height, m_Position.width * Screen.width, m_Position.height * Screen.height);
            GUI.Label(pixelPosition, text, style);
        }
    }
}
