using System.Collections.Generic;
using UnityEngine;

namespace FireReady.UI
{
    // Fills a layout group from a hidden template row and reuses rows between refreshes.
    public class RowList : MonoBehaviour
    {
        [SerializeField] RowView template;

        readonly List<RowView> rows = new List<RowView>();
        int used;

        public int Count => used;

        void Awake()
        {
            if (template != null) template.gameObject.SetActive(false);
        }

        public void Clear()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            used = 0;
        }

        public RowView Add()
        {
            RowView row;
            if (used < rows.Count)
            {
                row = rows[used];
            }
            else
            {
                row = Instantiate(template, template.transform.parent, false);
                rows.Add(row);
            }

            used++;
            row.transform.SetAsLastSibling();
            row.gameObject.SetActive(true);
            row.SetTitleColor(Color.white);
            if (row.Button != null) row.Button.onClick.RemoveAllListeners();
            return row;
        }
    }
}
