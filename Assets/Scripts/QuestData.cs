using System;

public static class QuestData
{
    public static event Action OnQuestsChanged;

    // Видимость этажей
    public static bool Floor3Visible { get; private set; }
    public static bool Floor2Visible { get; private set; }
    public static bool Floor1Visible { get; private set; }

    // Главные квесты
    public static bool Floor3MainDone { get; private set; }
    public static bool Floor2MainDone { get; private set; }
    public static bool Floor1MainDone { get; private set; }

    // Счётчики рычагов
    public static int Floor2LeversTotal { get; private set; }
    public static int Floor2LeversDone { get; private set; }
    public static int Floor1LeversTotal { get; private set; }
    public static int Floor1LeversDone { get; private set; }

    // Доп квест
    public static bool AntidoteVisible { get; private set; }
    public static bool AntidoteDone { get; private set; }

    public static void Reset()
    {
        Floor3Visible = false;
        Floor2Visible = false;
        Floor1Visible = false;
        Floor3MainDone = false;
        Floor2MainDone = false;
        Floor1MainDone = false;
        Floor2LeversDone = 0;
        Floor1LeversDone = 0;
        AntidoteVisible = false;
        AntidoteDone = false;
    }

    public static void RevealFloor3()
    {
        Floor3Visible = true;
        Notify();
    }

    public static void RevealFloor2(int leverCount)
    {
        Floor2Visible = true;
        Floor2LeversTotal = leverCount;
        AntidoteVisible = true;
        Notify();
    }

    public static void RevealFloor1(int leverCount)
    {
        Floor1Visible = true;
        Floor1LeversTotal = leverCount;
        Notify();
    }

    public static void CompleteFloor3Main()
    {
        Floor3MainDone = true;
        Notify();
    }

    public static void RegisterFloor2Lever()
    {
        Floor2LeversDone++;
        Notify();
    }

    public static void CompleteFloor2Main()
    {
        Floor2MainDone = true;
        Notify();
    }

    public static void RegisterFloor1Lever()
    {
        Floor1LeversDone++;
        Notify();
    }

    public static void CompleteFloor1Main()
    {
        Floor1MainDone = true;
        Notify();
    }

    public static void CompleteAntidote()
    {
        AntidoteDone = true;
        Notify();
    }

    static void Notify() => OnQuestsChanged?.Invoke();
}