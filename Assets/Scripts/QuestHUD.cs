using UnityEngine;
using TMPro;
using System.Text;

public class QuestHUD : MonoBehaviour
{
    public TextMeshProUGUI questText;

    void OnEnable()
    {
        QuestData.OnQuestsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        QuestData.OnQuestsChanged -= Refresh;
    }

    void Refresh()
    {
        var sb = new StringBuilder();

        if (QuestData.Floor3Visible)
        {
            sb.AppendLine("<color=#aaaaaa>— ЭТАЖ 3 —</color>");
            AppendMain(sb, "Найти спуск на 2 этаж", QuestData.Floor3MainDone);
            sb.AppendLine();
        }

        if (QuestData.Floor2Visible)
        {
            sb.AppendLine("<color=#aaaaaa>— ЭТАЖ 2 —</color>");
            AppendMain(sb, "Спуститься на 1 этаж", QuestData.Floor2MainDone);
            if (!QuestData.Floor2MainDone)
                AppendCounter(sb, "Активировать рычаги",
                    QuestData.Floor2LeversDone, QuestData.Floor2LeversTotal);

            if (QuestData.AntidoteVisible)
                AppendSide(sb, "Найти антидот", QuestData.AntidoteDone);
            sb.AppendLine();
        }

        if (QuestData.Floor1Visible)
        {
            sb.AppendLine("<color=#aaaaaa>— ЭТАЖ 1 —</color>");
            AppendMain(sb, "Выбраться наружу", QuestData.Floor1MainDone);
            if (!QuestData.Floor1MainDone)
                AppendCounter(sb, "Активировать рычаги",
                    QuestData.Floor1LeversDone, QuestData.Floor1LeversTotal);
        }

        questText.text = sb.ToString();
    }

    void AppendMain(StringBuilder sb, string label, bool done)
    {
        if (done)
            sb.AppendLine($"<color=#44ff44>✓ {label}</color>");
        else
            sb.AppendLine($"<color=#ffffff>▶ {label}</color>");
    }

    void AppendCounter(StringBuilder sb, string label, int done, int total)
    {
        string color = done >= total ? "#44ff44" : "#ffffff";
        sb.AppendLine($"<color={color}>   └ {label} ({done}/{total})</color>");
    }

    void AppendSide(StringBuilder sb, string label, bool done)
    {
        if (done)
            sb.AppendLine($"<color=#44ff44>✓ {label} [доп]</color>");
        else
            sb.AppendLine($"<color=#ffdd44>◆ {label} [доп]</color>");
    }
}