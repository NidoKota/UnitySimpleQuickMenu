using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleQuickMenu
{
    /// <summary>
    /// SimpleQuickMenuを制御するClass
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Animator)), RequireComponent(typeof(CanvasGroup))]
    public class SimpleQuickMenu : MonoBehaviour, IAnimationClipSource
    {
        /// <summary>
        /// 決定の処理を実行する時に呼ばれるCallBack(Transformはメニューの階層)
        /// </summary>
        public event Action<Transform> InvokeMenuCallBack;

        /// <summary>
        /// 現在選択中のMenuの階層
        /// </summary>
        public Transform CurrentMenuHierarchy { get; private set; }

        /// <summary>
        /// メニューが表示されているか
        /// </summary>
        public bool View { get; private set; }                                             

        [Header("Component")]
        [SerializeField] Image selecter = default;                      //選択を示すImage
        [SerializeField] TextMeshProUGUI titleText = default;           //タイトルを表示するTMPro
        [SerializeField] TextMeshProUGUI menuText = default;            //メニューを表示するTMPro

        [Header("Setting")]
        [SerializeField] Transform menuHierarchy = default;             //メニューの階層をGameObjectの階層で設定する
        [SerializeField] float lineHeight = 0;                          //メニューの1行の高さ
        [SerializeField] float lineSpaceHeight = 0;                     //行の間の空白の高さ
        [SerializeField] float firstSelectY = 0;                        //メニューの1行目のY座標
        [SerializeField] float multiplyTextLength = 0;                  //文字の長さに掛けてSelecterのX軸の大きさを調整する
        [SerializeField] float smooth = 0;                              //移動やフェードのスムーズ係数
        [SerializeField] float selectWaitTime = 0;                      //menuTextが移動したら数秒待つ
        [SerializeField] AnimationClip selectAnimationClip = default;   //menuTextが移動した時に再生するAnimationClip
        [SerializeField] AnimationClip decisionAnimationClip = default; //決定した時に再生するAnimationClip
        [SerializeField] AnimationClip cancelAnimationClip = default;   //キャンセルしたときに再生するAnimationClip

        [Header("AnimationProperty")]
        [SerializeField] float animationSelecterScaleX = 0;             //SelecterのX軸の大きさを、0に近いほど文字数の長さから計算された数値に、1に近いほどtargetScaleXの数値にする
        [SerializeField] float targetScaleX = 0;                        //animationSelecterScaleXが1の時のSelecterのX軸の大きさ
        [SerializeField] float addSelecterScaleX = 0;                   //SelecterのX軸の大きさに加算する

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] int selectPreviw = 0;                          //selecterの選択のプレビュー
        [SerializeField] bool viewState = false;                        //Stateの状態を表示する
#endif

        AnimationMixerPlayable animationMixerPlayable;                  //Animationの再生に使用するAnimationMixerPlayable
        AnimationPlayableOutput animOutput;                             //Animationの再生に使用するAnimationPlayableOutput
        PlayableGraph animaionGraph;                                    //Animationの再生に使用するPlayableGraph
        State state;                                                    //現在の状態を表すステート
        CanvasGroup canvasGroup;                                        //メニュー全体をフェードさせる
        MatchCollection menuTextLineChecker;                            //menuTextを行ごとに分ける
        bool waitOnce;                                                  //menuTextが移動したら一度だけ待つ
        float selectWaitTimer;                                          //menuTextが移動したら時間を計る
        int menuTextLineCount;                                          //menuTextの行数
        int SelectLine                                                  //選択中の行数
        {
            get { return CurrentMenuHierarchy.GetSiblingIndex(); }
            set { CurrentMenuHierarchy = CurrentMenuHierarchy.parent.GetChild(value); }
        }

        void Start()
        {
#if UNITY_EDITOR
            //ゲーム上でのみ実行する
            if (EditorApplication.isPlaying)
#endif
            {
                //Component取得
                canvasGroup = GetComponent<CanvasGroup>();

                //Animationの再生に使用するPlayebleの設定
                animaionGraph = PlayableGraph.Create("QuickMenuAnimationPlayable");
                animaionGraph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
                animOutput = AnimationPlayableOutput.Create(animaionGraph, "QuickMenuAnimationPlayableOutput", GetComponent<Animator>());
                animationMixerPlayable = AnimationMixerPlayable.Create(animaionGraph, 3, false);
                animationMixerPlayable.ConnectInput(0, AnimationClipPlayable.Create(animaionGraph, selectAnimationClip), 0);
                animationMixerPlayable.ConnectInput(1, AnimationClipPlayable.Create(animaionGraph, decisionAnimationClip), 0);
                animationMixerPlayable.ConnectInput(2, AnimationClipPlayable.Create(animaionGraph, cancelAnimationClip), 0);
                animOutput.SetSourcePlayable(animationMixerPlayable);
                animOutput.SetSourceOutputPort(0);
                animaionGraph.Play();

                //初期化
                canvasGroup.alpha = 0;
                selecter.transform.localScale = new Vector3(0, selecter.transform.localScale.y, selecter.transform.localScale.z);
                CurrentMenuHierarchy = menuHierarchy.GetChild(0);
                UpdateMenu();
            }
        }

        //AnimationでPropertyを変更してから計算するためにLateUpdateにする
        void LateUpdate()
        {
#if UNITY_EDITOR
            //ゲーム上でのみ実行する
            if (EditorApplication.isPlaying)
#endif
            {
                //メニューが表示されている時
                if (View)
                {
                    //選択している行の文字列の横の大きさを計算
                    float textScale = GetTextLength(menuTextLineChecker[SelectLine].Groups["v"].Value, menuText.font);
                    //AnimationPropertyの値からSelecterのX軸の大きさを計算する
                    selecter.transform.localScale = new Vector3((targetScaleX - textScale) * animationSelecterScaleX + textScale + addSelecterScaleX, selecter.transform.localScale.y, selecter.transform.localScale.z);

                    //選択中
                    if (state == State.Select)
                    {
                        //menuTextを移動させたら指定時間待つ
                        if (waitOnce)
                        {
                            selectWaitTimer += Time.unscaledDeltaTime;
                            if (selectWaitTimer >= selectWaitTime)
                            {
                                selectWaitTimer = 0;
                                waitOnce = false;
                            }
                        }
                        else
                        {
                            //上に移動させる
                            if (Input.GetKey(KeyCode.UpArrow))
                            {
                                PlayAnimation(selectAnimationClip);

                                //一番下から場外へ移動する時
                                if (0 > SelectLine - 1) SelectLine = menuTextLineCount - 1;
                                else SelectLine--;
                                waitOnce = true;
                            }
                            //下に移動させる
                            else if (Input.GetKey(KeyCode.DownArrow))
                            {
                                PlayAnimation(selectAnimationClip);

                                //一番上から場外へ移動する時
                                if (menuTextLineCount <= SelectLine + 1) SelectLine = 0;
                                else SelectLine++;
                                waitOnce = true;
                            }

                        }

                        //決定
                        if (Input.GetKey(KeyCode.Space))
                        {
                            SimpleQuickMenuLine simpleQuickMenuLine = CurrentMenuHierarchy.GetComponent<SimpleQuickMenuLine>();
                            if (simpleQuickMenuLine && simpleQuickMenuLine.back)
                            {
                                PlayAnimation(cancelAnimationClip);
                                state = State.Cancel;
                            }
                            else
                            {
                                PlayAnimation(decisionAnimationClip);
                                state = State.Decision;
                            }
                        }

                        //キャンセル
                        if (Input.GetKey(KeyCode.Escape))
                        {
                            PlayAnimation(cancelAnimationClip);
                            state = State.Cancel;
                        }

                        //現在選択している行にSelecterが重なるようにmenuTextを滑らかに移動させる
                        menuText.transform.localPosition = Vector3.up * Mathf.Lerp(menuText.transform.localPosition.y, firstSelectY + lineHeight * SelectLine + lineSpaceHeight * (SelectLine <= 0 ? 0 : SelectLine - 1), smooth * Time.unscaledDeltaTime)
                        + Vector3.right * menuText.transform.localPosition.x + Vector3.forward * menuText.transform.localPosition.z;

                        //行の色を変える
                        for (int line = 0; line < menuTextLineCount; line++)
                        {
                            //変更する文字列が始まる位置と終わる位置を取得
                            int start = menuTextLineChecker[line].Groups["v"].Index;
                            int end = start + menuTextLineChecker[line].Groups["v"].Value.Count() - 1;

                            //真ん中に行くほど色を濃くする
                            if (line - SelectLine == -2) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, new Color(1, 1, 1, 0.7f), new Color(1, 1, 1, 0.2f), new Color(1, 1, 1, 0.2f), new Color(1, 1, 1, 0.7f));
                            if (line - SelectLine == -1) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0.8f), new Color(1, 1, 1, 0.8f), new Color(1, 1, 1, 0.9f));
                            if (line - SelectLine == 0) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, Color.black, Color.black, Color.black, Color.black);
                            if (line - SelectLine == 1) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, new Color(1, 1, 1, 0.8f), new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0.8f));
                            if (line - SelectLine == 2) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, new Color(1, 1, 1, 0.2f), new Color(1, 1, 1, 0.7f), new Color(1, 1, 1, 0.7f), new Color(1, 1, 1, 0.2f));
                            if (line - SelectLine <= -3 || line - SelectLine >= 3) SetTextColor(menuText, start, end, smooth * Time.unscaledDeltaTime, Color.clear, Color.clear, Color.clear, Color.clear);
                        }
                        //canvasGroupとtitleTextをフェードイン
                        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1, 6 * Time.unscaledDeltaTime);
                        titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, Mathf.Lerp(titleText.color.a, 1, smooth * Time.unscaledDeltaTime));

                    }
                    //決定またはキャンセルした時
                    else if (state == State.Decision || state == State.Cancel)
                    {
                        //Animation再生が完了した時
                        if (state == State.Decision && animationMixerPlayable.GetInput(1).GetTime() >= decisionAnimationClip.length || state == State.Cancel && animationMixerPlayable.GetInput(2).GetTime() >= cancelAnimationClip.length)
                        {
                            //決定したかつ、次に進む項目がない時はフェードを行わない
                            if (state == State.Decision && CurrentMenuHierarchy.childCount == 0)
                            {
                                //登録された処理を実行
                                SimpleQuickMenuLine simpleQuickMenuLine = CurrentMenuHierarchy.GetComponent<SimpleQuickMenuLine>();
                                if (simpleQuickMenuLine) simpleQuickMenuLine.unityEvent.Invoke();
                                if (InvokeMenuCallBack != null) InvokeMenuCallBack.Invoke(CurrentMenuHierarchy);

                                PlayAnimation(selectAnimationClip);
                                state = State.Select;
                            }
                            else
                            {
                                //menuTextとtitleTextをフェードアウト
                                SetTextColor(menuText, 0, menuText.text.Count() - 1, smooth * Time.unscaledDeltaTime, Color.clear, Color.clear, Color.clear, Color.clear);
                                titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, Mathf.Lerp(titleText.color.a, 0, smooth * Time.unscaledDeltaTime));

                                //フェードアウトが終わった時
                                if (titleText.color.a <= 0.05f)
                                {
                                    //後ろの項目に戻る
                                    if (state == State.Cancel)
                                    {
                                        //後ろに戻る項目がない時メニューを消す
                                        if (CurrentMenuHierarchy.parent == menuHierarchy)
                                        {
                                            SelectLine = 0;

                                            UpdateMenu();
                                            PlayAnimation(null);
                                            state = State.Select;
                                            Time.timeScale = 1;
                                            View = false;
                                        }
                                        //後ろの項目に戻る
                                        else
                                        {
                                            CurrentMenuHierarchy = CurrentMenuHierarchy.parent;
                                            SelectLine = 0;

                                            UpdateMenu();
                                            PlayAnimation(selectAnimationClip);
                                            state = State.Select;
                                        }
                                    }
                                    //次の項目に進む
                                    else if (state == State.Decision)
                                    {
                                        //登録された処理を実行
                                        SimpleQuickMenuLine simpleQuickMenuLine = CurrentMenuHierarchy.GetComponent<SimpleQuickMenuLine>();
                                        if (simpleQuickMenuLine) simpleQuickMenuLine.unityEvent.Invoke();
                                        if (InvokeMenuCallBack != null) InvokeMenuCallBack.Invoke(CurrentMenuHierarchy);

                                        CurrentMenuHierarchy = CurrentMenuHierarchy.GetChild(0);
                                        SelectLine = 0;

                                        UpdateMenu();
                                        PlayAnimation(selectAnimationClip);
                                        state = State.Select;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //canvasGroupをフェードアウト
                    canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0, 6 * Time.unscaledDeltaTime);

                    //(GetKeyUpにしているのは1F判定じゃないとメニュー上で勝手に決定されてしまうから
                    //GetKeyDownにしないのは、メニュー上の決定の判定がGetKeyであるから
                    //メニュー上の決定の判定がGetKeyなのは、Time.timeScaleが0の時はUpとDownが判定されないから
                    //この問題は新しいImputSystemを使う事で解決するが、あまり流行っていないのでこちらにした)
                    if (Input.GetKeyUp(KeyCode.Escape))
                    {
                        //メニューを表示させる
                        Time.timeScale = 0;
                        View = true;
                        PlayAnimation(selectAnimationClip);
                    }
                }
            }

#if UNITY_EDITOR
            //Editor上でしか実行しない(Animationの確認用)
            else
            {
                if (menuText && selecter && menuHierarchy)
                {
                    titleText.text = menuHierarchy.name;
                    menuText.text = GetMenuText(menuHierarchy);
                    menuTextLineChecker = new Regex("(?<v>.+)\n?").Matches(menuText.text);
                    menuTextLineCount = menuTextLineChecker.Count;

                    selectPreviw = Mathf.Clamp(selectPreviw, 0, menuTextLineCount - 1);

                    menuText.transform.localPosition = Vector3.up * (firstSelectY + lineHeight * selectPreviw + lineSpaceHeight * (selectPreviw <= 0 ? 0 : selectPreviw - 1)) + Vector3.right * menuText.transform.localPosition.x + Vector3.forward * menuText.transform.localPosition.z;

                    //プレビューする行の文字列の横の大きさを計算
                    float textScale = GetTextLength(menuTextLineChecker[selectPreviw].Groups["v"].Value, menuText.font);

                    //AnimationWindowがプレビュー状態の時はAnimationPropertyの値を適応してSelecterのX軸の大きさを計算する
                    if (AnimationMode.InAnimationMode()) selecter.transform.localScale = new Vector3((targetScaleX - textScale) * animationSelecterScaleX + textScale + addSelecterScaleX, selecter.transform.localScale.y, selecter.transform.localScale.z);
                    else selecter.transform.localScale = new Vector3(textScale, selecter.transform.localScale.y, selecter.transform.localScale.z);
                }
            }
#endif
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            //ステートを表示する
            if (viewState) GUILayout.Box($"State {state}", GUILayout.ExpandWidth(false));
        }
#endif

        /// <summary>
        /// メニューの内容を更新する
        /// </summary>
        void UpdateMenu()
        {
            titleText.text = CurrentMenuHierarchy.parent.name;
            menuText.text = GetMenuText(CurrentMenuHierarchy.parent);
            menuTextLineChecker = new Regex("(?<v>.+)\n?").Matches(menuText.text);
            menuTextLineCount = menuTextLineChecker.Count;
            //menuTextのtextInfoを強制的に更新させる
            menuText.ForceMeshUpdate();

            menuText.transform.localPosition = Vector3.up * (firstSelectY + lineHeight * SelectLine + lineSpaceHeight * (SelectLine <= 0 ? 0 : SelectLine - 1)) + Vector3.right * menuText.transform.localPosition.x + Vector3.forward * menuText.transform.localPosition.z;
            SetTextColor(menuText, 0, menuText.text.Count() - 1, 1, Color.clear, Color.clear, Color.clear, Color.clear);
        }

        /// <summary>
        /// GameObjectの階層から現在menuTextに表示すべきTextを取得する
        /// </summary>
        string GetMenuText(Transform parent)
        {
            string s = default;
            bool first = true;
            foreach (Transform child in parent)
            {
                s += first ? child.name : "\n" + child.name;
                first = false;
            }
            return s;
        }

        /// <summary>
        /// 指定した文字の色を変更する
        /// </summary>
        void SetTextColor(TMP_Text tmp, int start, int end, float lerp, Color leftDown, Color leftUp, Color rightUp, Color rightDown)
        {
            //1文字ごとに処理を行う
            for (int index = start; index <= end; index++)
            {
                Color32[] vertexColors = tmp.textInfo.meshInfo[tmp.textInfo.characterInfo[index].materialReferenceIndex].colors32;

                int vertexIndex = tmp.textInfo.characterInfo[index].vertexIndex;

                //一応表示されているか確認する
                if (tmp.textInfo.characterInfo[index].isVisible)
                {
                    vertexColors[vertexIndex + 0] = Color32.Lerp(vertexColors[vertexIndex + 0], new Color(leftDown.r, leftDown.g, leftDown.b, leftDown.a), lerp);
                    vertexColors[vertexIndex + 1] = Color32.Lerp(vertexColors[vertexIndex + 1], new Color(leftUp.r, leftUp.g, leftUp.b, leftUp.a), lerp);
                    vertexColors[vertexIndex + 2] = Color32.Lerp(vertexColors[vertexIndex + 2], new Color(rightUp.r, rightUp.g, rightUp.b, rightUp.a), lerp);
                    vertexColors[vertexIndex + 3] = Color32.Lerp(vertexColors[vertexIndex + 3], new Color(rightDown.r, rightDown.g, rightDown.b, rightDown.a), lerp);
                }
                //文字の色を更新させる
                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }

        /// <summary>
        /// アニメーションをはじめから再生する
        /// </summary>
        void PlayAnimation(AnimationClip animationClip)
        {
            for (int i = 0; i < animationMixerPlayable.GetInputCount(); i++)
            {
                //animationMixerPlayableの入力から同じanimationClipを持ったAnimationClipPlayableを探す
                if (((AnimationClipPlayable)animationMixerPlayable.GetInput(i)).GetAnimationClip() == animationClip)
                {
                    animationMixerPlayable.SetInputWeight(i, 1);
                    animationMixerPlayable.GetInput(i).SetTime(0);
                }
                else animationMixerPlayable.SetInputWeight(i, 0);
            }
        }

        /// <summary>
        /// 文字列の横の長さを取得する
        /// </summary>
        //TODO:文字によって微妙にズレが出る問題
        float GetTextLength(string text, TMP_FontAsset font)
        {
            float length = 0;

            //1文字ごとに処理を行う
            for (int index = 0; index < text.Count(); index++)
            {
                //Unicodeを使ってFontAssetから文字を検索する
                byte[] data = System.Text.Encoding.Unicode.GetBytes(text[index].ToString());
                TMP_Character character = font.characterTable.FirstOrDefault(x => x.unicode == BitConverter.ToUInt16(data, 0));

                //検索した文字のhorizontalAdvanceを足し合わせる
                if (character != null) length += character.glyph.metrics.horizontalAdvance;
            }

            return length * multiplyTextLength;
        }

        //AnimationWindowでプレビューするAnimationを渡す(IAnimationClipSourceInterfaceの規約)
        public void GetAnimationClips(List<AnimationClip> results)
        {
            if (selectAnimationClip) results.Add(selectAnimationClip);
            if (decisionAnimationClip) results.Add(decisionAnimationClip);
            if (cancelAnimationClip) results.Add(cancelAnimationClip);
        }

        void OnDestroy()
        {
            //使い終わったら解放
            if (animaionGraph.IsValid()) animaionGraph.Destroy();
        }

        /// <summary>
        /// 現在のステート
        /// </summary>
        enum State
        {
            /// <summary>
            /// 選んでいる時
            /// </summary>
            Select,
            /// <summary>
            /// 決定して次のメニューに移行している時
            /// </summary>
            Decision,
            /// <summary>
            /// キャンセルして前のメニューに戻る時
            /// </summary>
            Cancel
        }
    }
}