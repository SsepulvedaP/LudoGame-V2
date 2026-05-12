using System;
using UnityEngine;

/// <summary>
/// DTOs alineados con /openapi.json (BartleSurveyOut, BartleSummaryOut, etc.).
/// </summary>
[Serializable]
public class BartleSurveyOut
{
    public BartleQuestionOut[] questions;
}

[Serializable]
public class BartleQuestionOut
{
    public int id;
    public string prompt;
    public int sort_order;
    public BartleOptionOut[] options;
}

[Serializable]
public class BartleOptionOut
{
    public int id;
    public int option_index;
    public string bartle_type;
    public string label;
}

[Serializable]
public class BartleSummaryOut
{
    public BartleCountsOut counts;
    public int answered_questions;
    public string dominant_type;
}

[Serializable]
public class BartleCountsOut
{
    public int Killer;
    public int Socializer;
    public int Achiever;
    public int Explorer;
}

[Serializable]
public class UserBartleAnswerSubmit
{
    public int user_id;
    public int question_id;
    public int option_id;
}
