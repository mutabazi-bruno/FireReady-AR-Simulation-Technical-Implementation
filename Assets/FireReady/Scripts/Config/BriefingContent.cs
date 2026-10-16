using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Config
{
    // Text and pictures for the briefing pages (FR-02). Lives in the Core Addressables group.
    [CreateAssetMenu(fileName = "BriefingContent", menuName = "FireReady/Briefing Content")]
    public class BriefingContent : ScriptableObject
    {
        [Serializable]
        public class Item
        {
            public string heading;
            [TextArea] public string text;
            public Sprite icon;
            public Color color = Color.white;
        }

        [Serializable]
        public class Page
        {
            public string title;
            [TextArea(3, 8)] public string body;
            public List<Item> items = new List<Item>();
        }

        public List<Page> pages = new List<Page>();
    }
}
