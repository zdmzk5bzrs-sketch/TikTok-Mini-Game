using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaravanGame : MonoBehaviour
{
    enum ScreenId { Home, Map, Loadout, Battle, Result, Shop, Missions, Settings }

    readonly string[] gear = { "🏹 قوس", "🛡️ درع", "🔥 مشعل", "🪓 فأس", "💧 ماء", "🧭 بوصلة", "🗡️ سيف", "🪙 ذهب" };
    readonly string[] stages = { "بوابة نجد", "وادي الرمال", "سوق الحجاز", "طريق البحر", "حصن الشمال" };

    readonly HashSet<int> selected = new HashSet<int>();

    Canvas canvas;
    RectTransform root;
    Text toast;
    Text battleStatus;
    int coins = 120;
    int level = 1;
    int weight;
    int wave;
    int enemiesLeft;
    int caravanHp = 3;
    int defeatedThisRun;
    bool resultShown;
    bool battleWon;
    float enemyAttackTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindObjectOfType<CaravanGame>()) return;
        var g = new GameObject("CaravanGame");
        DontDestroyOnLoad(g);
        g.AddComponent<CaravanGame>();
    }

    void Awake()
    {
        LoadProgress();

        var g = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = g.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        root = Make("Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        Show(ScreenId.Home);
    }

    RectTransform Make(string name, Transform parent)
    {
        var g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        return g.GetComponent<RectTransform>();
    }

    void Clear()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
        toast = null;
        battleStatus = null;
    }

    void Show(ScreenId id)
    {
        Clear();

        switch (id)
        {
            case ScreenId.Home: Home(); break;
            case ScreenId.Map: Map(); break;
            case ScreenId.Loadout: Loadout(); break;
            case ScreenId.Battle: StartBattle(); break;
            case ScreenId.Result: Result(); break;
            case ScreenId.Shop: Shop(); break;
            case ScreenId.Missions: Missions(); break;
            case ScreenId.Settings: Settings(); break;
        }
    }

    Text T(Transform parent, string text, int size)
    {
        var g = new GameObject("Text", typeof(Text));
        g.transform.SetParent(parent, false);
        var t = g.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    Button B(Transform parent, string text, int size)
    {
        var g = new GameObject("Button", typeof(Image), typeof(Button));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = new Color(.19f, .14f, .09f);

        var b = g.GetComponent<Button>();
        var t = T(g.transform, text, size);
        R(t, 0, 0, 1000, 100);
        return b;
    }

    void R(Graphic graphic, float x, float y, float w, float h)
    {
        var r = graphic.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    void Header(string title, string subtitle)
    {
        var t = T(root, title, 38);
        R(t, 0, 790, 1080, 70);

        var c = T(root, "🪙 " + coins, 25);
        R(c, 380, 790, 300, 55);

        var s = T(root, subtitle, 18);
        s.color = new Color(.7f, .64f, .54f);
        R(s, 0, 720, 1080, 50);
    }

    void Add(string text, UnityEngine.Events.UnityAction action, ref float y)
    {
        var b = B(root, text, 27);
        R(b, 0, y, 900, 82);
        b.onClick.AddListener(action);
        y -= 100;
    }

    void Home()
    {
        Header("🏜️ حارس القافلة", "طريق التجارة يبدأ من هنا");

        var intro = T(root, "قافلة نجد\n\nاحمِ القافلة وطوّر معداتك وافتح طرقًا جديدة.", 34);
        R(intro, 0, 410, 1080, 300);

        float y = 150;
        Add("ابدأ الرحلة ⚔️", () => Show(ScreenId.Map), ref y);
        Add("التجهيز 🎒", () => Show(ScreenId.Loadout), ref y);
        Add("المتجر 🛒", () => Show(ScreenId.Shop), ref y);
        Add("المهام 📜", () => Show(ScreenId.Missions), ref y);
        Add("الإعدادات ⚙️", () => Show(ScreenId.Settings), ref y);
    }

    void Map()
    {
        Header("🗺️ خريطة القوافل", "افتح الطرق واحدة تلو الأخرى");

        float y = 520;
        for (int i = 0; i < stages.Length; i++)
        {
            int stage = i;
            bool unlocked = stage < level;

            var b = B(root, (unlocked ? "🟢 " : "🔒 ") + stages[stage] + " · المرحلة " + (stage + 1), 23);
            R(b, 0, y, 980, 100);

            if (unlocked)
                b.onClick.AddListener(() => Show(ScreenId.Loadout));

            y -= 120;
        }

        var h = B(root, "الرئيسية", 22);
        R(h, 0, -780, 900, 80);
        h.onClick.AddListener(() => Show(ScreenId.Home));
    }

    void Loadout()
    {
        Header("🎒 تجهيز القافلة", "سعة الحمولة 10 — اختر أدواتك");

        var c = T(root, "السعة: " + weight + " / 10", 24);
        R(c, 0, 650, 1000, 55);

        float y = 550;
        for (int i = 0; i < gear.Length; i++)
        {
            int index = i;
            var b = B(root, (selected.Contains(i) ? "✓ " : "") + gear[i], 22);
            R(b, 0, y, 980, 82);
            b.onClick.AddListener(() => ToggleGear(index));
            y -= 92;
        }

        var start = B(root, weight >= 6 ? "ابدأ الدفاع ⚔️" : "اختر معدات أكثر (6 على الأقل)", 26);
        R(start, 0, -650, 980, 90);
        start.onClick.AddListener(() =>
        {
            if (weight >= 6) Show(ScreenId.Battle);
            else ShowToast("اختر معدات بوزن 6 على الأقل");
        });
    }

    void ToggleGear(int index)
    {
        int w = GearWeight(index);

        if (selected.Contains(index))
        {
            selected.Remove(index);
            weight -= w;
        }
        else if (weight + w <= 10)
        {
            selected.Add(index);
            weight += w;
        }
        else
        {
            ShowToast("الحمولة ممتلئة");
            return;
        }

        Show(ScreenId.Loadout);
    }

    int GearWeight(int index)
    {
        if (index == 1 || index == 3) return 3;
        if (index == 4 || index == 5) return 1;
        return 2;
    }

    void Update()
    {
        if (!battleActive) return;
        enemyAttackTimer += Time.deltaTime;
        if (enemyAttackTimer >= 4f)
        {
            enemyAttackTimer = 0f;
            caravanHp--;
            if (caravanHp <= 0)
            {
                battleActive = false;
                battleWon = false;
                resultShown = true;
                Show(ScreenId.Result);
                return;
            }
            RefreshBattleStatus();
        }
    }

    bool battleActive;

    void StartBattle()
    {
        wave = 1;
        enemiesLeft = 3;
        caravanHp = 3;
        defeatedThisRun = 0;
        resultShown = false;
        battleWon = false;
        battleActive = true;
        enemyAttackTimer = 0f;
        RenderBattle();
    }

    void RenderBattle()
    {
        Clear();

        Header("⚔️ الدفاع عن القافلة", "اضغط الغزاة قبل وصولهم");

        battleStatus = T(root, "❤️ " + caravanHp + "     موجة " + wave + " / 3     أعداء: " + enemiesLeft, 25);
        R(battleStatus, 0, 650, 1000, 60);

        var scene = T(root, "🌙\n\n🏰                         🐪\n\n      ⚔️        🦂        ⚔️", 48);
        R(scene, 0, 150, 1000, 800);

        float x = -280;
        string[] enemies = { "⚔️", "🦂", "⚔️" };

        for (int i = 0; i < enemies.Length; i++)
        {
            int enemyIndex = i;
            var b = B(root, enemies[i], 45);
            R(b, x, -250, 180, 120);
            b.onClick.AddListener(() => HitEnemy(b, enemyIndex));
            x += 280;
        }

        var retreat = B(root, "انسحاب", 22);
        R(retreat, 0, -700, 980, 80);
        retreat.onClick.AddListener(() => Show(ScreenId.Home));
    }

    void HitEnemy(Button enemyButton, int enemyIndex)
    {
        if (enemyButton == null) return;

        defeatedThisRun++;
        enemiesLeft = Mathf.Max(0, enemiesLeft - 1);
        Destroy(enemyButton.gameObject);

        if (enemiesLeft > 0)
        {
            SaveProgress();
            RefreshBattleStatus();
            return;
        }

        if (wave < 3)
        {
            wave++;
            enemiesLeft = 3;
            SaveProgress();
            RenderBattle();
            return;
        }

        resultShown = true;
        battleWon = true;
        battleActive = false;
        SaveProgress();
        Show(ScreenId.Result);
    }

    void RefreshBattleStatus()
    {
        if (battleStatus == null) return;
        battleStatus.text = "❤️ " + caravanHp + "     موجة " + wave + " / 3     أعداء: " + enemiesLeft;
    }

    void Result()
    {
        if (battleWon)
        {
            Header("🏆 وصلت القافلة", "الجولة اكتملت");
            var reward = 50 + defeatedThisRun * 10;
            var t = T(root, "✨\n\nغنيمة الطريق\n+" + reward + " 🪙\n\nهزمت " + defeatedThisRun + " من الغزاة", 36);
            R(t, 0, 300, 1000, 420);

            if (resultShown)
            {
                coins += reward;
                level = Mathf.Min(5, level + 1);
                resultShown = false;
                SaveProgress();
            }
        }
        else
        {
            Header("💥 سقطت القافلة", "حاول مرة أخرى");
            var t = T(root, "القافلة تعرضت للهجوم\n\nهزمت " + defeatedThisRun + " من الغزاة", 36);
            R(t, 0, 300, 1000, 300);
            resultShown = false;
        }

        var next = B(root, battleWon ? "التالي →" : "إعادة المحاولة ↻", 28);
        R(next, 0, -300, 980, 90);
        next.onClick.AddListener(() => battleWon ? Show(ScreenId.Map) : Show(ScreenId.Loadout));
    }

    void Shop()
    {
        Header("🛒 سوق القافلة", "مظاهر وأغراض تجميلية");

        string[] items =
        {
            "🏜️ مظهر الصحراء — 80 🪙",
            "🚩 راية القافلة — 120 🪙",
            "🛡️ درع ذهبي — 180 🪙",
            "🐪 جمل أسود — 250 🪙"
        };

        int[] prices = { 80, 120, 180, 250 };
        float y = 520;

        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            var b = B(root, items[i], 23);
            R(b, 0, y, 980, 100);
            b.onClick.AddListener(() => BuyItem(index, prices[index]));
            y -= 120;
        }

        var h = B(root, "رجوع", 22);
        R(h, 0, -780, 900, 80);
        h.onClick.AddListener(() => Show(ScreenId.Home));
    }

    void BuyItem(int index, int price)
    {
        if (coins < price)
        {
            ShowToast("عملاتك غير كافية");
            return;
        }

        coins -= price;
        SaveProgress();
        ShowToast("تم الشراء");
    }

    void Missions()
    {
        Header("📜 المهام", "مكافآت وتقدم مستمر");

        string[] missions =
        {
            "أكمل 3 جولات — 100 🪙",
            "اهزم 15 غازيًا — 150 🪙",
            "افتح طريقًا جديدًا — 200 🪙",
            "احمِ القافلة دون خسارة — ⭐"
        };

        float y = 520;
        foreach (var mission in missions)
        {
            var b = B(root, mission, 22);
            R(b, 0, y, 980, 100);
            y -= 120;
        }

        var h = B(root, "رجوع", 22);
        R(h, 0, -780, 900, 80);
        h.onClick.AddListener(() => Show(ScreenId.Home));
    }

    void Settings()
    {
        Header("⚙️ الإعدادات", "الصوت واللغة وإدارة البيانات");

        string[] options =
        {
            "🔊 الصوت: تشغيل",
            "📳 الاهتزاز: تشغيل",
            "🌐 اللغة: العربية",
            "💾 الحفظ: تلقائي",
            "🗑️ إعادة التقدم"
        };

        float y = 520;
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            var b = B(root, options[i], 22);
            R(b, 0, y, 980, 100);

            if (index == 4)
                b.onClick.AddListener(ResetProgress);

            y -= 120;
        }

        var h = B(root, "رجوع", 22);
        R(h, 0, -780, 900, 80);
        h.onClick.AddListener(() => Show(ScreenId.Home));
    }

    void ResetProgress()
    {
        coins = 120;
        level = 1;
        selected.Clear();
        weight = 0;
        SaveProgress();
        Show(ScreenId.Home);
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("caravan_coins", coins);
        PlayerPrefs.SetInt("caravan_level", level);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        coins = PlayerPrefs.GetInt("caravan_coins", 120);
        level = PlayerPrefs.GetInt("caravan_level", 1);
    }

    void ShowToast(string message)
    {
        if (toast != null) Destroy(toast.gameObject);

        toast = T(root, message, 25);
        toast.color = Color.white;
        R(toast, 0, -560, 850, 70);

        Destroy(toast.gameObject, 1.2f);
    }
}
