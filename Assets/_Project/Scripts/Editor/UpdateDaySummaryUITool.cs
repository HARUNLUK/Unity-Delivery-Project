using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class UpdateDaySummaryUITool
{
    [MenuItem("Tools/UI/Update Day Summary List Row UI")]
    public static void UpdateDaySummaryListUI()
    {
        // Search for DaySummaryPanel in open scene
        DaySummaryManager summaryMgr = Object.FindAnyObjectByType<DaySummaryManager>();
        if (summaryMgr == null)
        {
            Debug.LogWarning("[UpdateDaySummaryUITool] No DaySummaryManager found in active scene.");
            EditorUtility.DisplayDialog("Day Summary UI", "Aktif sahnede DaySummaryManager bulunamadı!", "Tamam");
            return;
        }

        Transform templateTrans = null;
        if (summaryMgr.historyItemTemplate != null)
        {
            templateTrans = summaryMgr.historyItemTemplate.transform;
        }
        else if (summaryMgr.historyListContent != null)
        {
            templateTrans = summaryMgr.historyListContent.Find("HistoryRowTemplate");
            if (templateTrans == null) templateTrans = summaryMgr.historyListContent.Find("HistoryItemTemplate");
        }

        if (templateTrans == null)
        {
            Debug.LogWarning("[UpdateDaySummaryUITool] HistoryRowTemplate not found!");
            EditorUtility.DisplayDialog("Day Summary UI", "HistoryRowTemplate şablon objesi bulunamadı!", "Tamam");
            return;
        }

        Undo.RecordObject(templateTrans.gameObject, "Update Day Summary Row Height");

        // 1. Adjust Row Height to 72px for comfortable 2-line rendering (Recipient+Address+Express on line 1, Delivered+Status on line 2)
        RectTransform rt = templateTrans.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 72f);
        }

        // LayoutElement if present
        LayoutElement le = templateTrans.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredHeight = 72f;
            le.minHeight = 72f;
        }

        // 2. Adjust Text Component
        TextMeshProUGUI rowTmp = templateTrans.GetComponentInChildren<TextMeshProUGUI>(true);
        if (rowTmp != null)
        {
            Undo.RecordObject(rowTmp, "Update Day Summary Row Text Settings");
            rowTmp.fontSize = 14f;
            rowTmp.enableWordWrapping = true;
            rowTmp.lineSpacing = 6f;
            rowTmp.alignment = TextAlignmentOptions.MidlineLeft;
            rowTmp.margin = new Vector4(14, 4, 14, 4);

            RectTransform textRt = rowTmp.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(templateTrans.gameObject.scene);
        Debug.Log("[UpdateDaySummaryUITool] Day Summary Row UI updated successfully!");
        EditorUtility.DisplayDialog("Day Summary UI Güncellendi", "Gün Sonu teslimat satırları (Alıcı Adı + Adres + Express Saati) için şablon başarıyla optimize edildi!", "Harika");
    }
}
