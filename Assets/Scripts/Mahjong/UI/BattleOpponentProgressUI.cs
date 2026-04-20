using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleOpponentProgressUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject battleRoot;

        [Header("Links")]
        [SerializeField] private Board board;
        [SerializeField] private TMP_Text opponentScoreText;
        [SerializeField] private TMP_Text opponentStatusText;

        [Header("Move FX")]
        [SerializeField] private RectTransform moveFxRoot;
        [SerializeField] private Image leftTileFx;
        [SerializeField] private Image rightTileFx;
        [SerializeField] private TMP_Text gainText;

        [Header("Tile FX Sprites")]
        [SerializeField] private Sprite defaultTileSprite;
        [SerializeField] private Sprite[] tileFxSprites;

        [Header("Text")]
        [SerializeField] private string scorePrefix = "Score: ";
        [SerializeField] private string statusSearching = "Thinking...";
        [SerializeField] private string statusPlaying = "Matching...";
        [SerializeField] private string statusPressure = "On fire...";
        [SerializeField] private string statusFinished = "Finished";

        [Header("Score Range")]
        [SerializeField] private int minStartScore = 40;
        [SerializeField] private int maxStartScore = 180;
        [SerializeField] private int minBurstGain = 20;
        [SerializeField] private int maxBurstGain = 120;

        [Header("Tempo")]
        [SerializeField, Range(0f, 1f)] private float minTempo = 0.02f;
        [SerializeField, Range(0f, 1f)] private float maxTempo = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float minBurstTempo = 0.01f;
        [SerializeField, Range(0f, 0.3f)] private float maxBurstTempo = 0.05f;

        [Header("Timing")]
        [SerializeField] private float minThinkDelay = 0.55f;
        [SerializeField] private float maxThinkDelay = 1.45f;
        [SerializeField] private float minBurstDelay = 0.18f;
        [SerializeField] private float maxBurstDelay = 0.45f;
        [SerializeField] private float finishAnimTime = 0.4f;

        [Header("Move Animation")]
        [SerializeField] private float tileMoveTime = 0.22f;
        [SerializeField] private float tileFadeTime = 0.12f;
        [SerializeField] private float tileOffsetX = 70f;
        [SerializeField] private float gainRiseY = 36f;

        [Header("Difficulty")]
        [SerializeField] private float bronzeSpeed = 0.90f;
        [SerializeField] private float silverSpeed = 1.00f;
        [SerializeField] private float goldSpeed = 1.08f;
        [SerializeField] private float jadeSpeed = 1.16f;
        [SerializeField] private float masterSpeed = 1.24f;

        private Coroutine simulationRoutine;
        private Coroutine moveFxRoutine;
        private bool battleActive;
        private bool finished;
        private int currentScore;
        private float hiddenTempo;
        private float speedFactor = 1f;

        private void Awake()
        {
            if (board == null)
                board = FindAnyObjectByType<Board>();

            HideMoveFxImmediate();
        }

        private void OnEnable()
        {
            Setup();
        }

        private void OnDisable()
        {
            UnbindBoard();
            StopSimulation();
            StopMoveFx();
            HideMoveFxImmediate();
        }

        private void Setup()
        {
            bool isBattle = MahjongSession.LaunchMode == MahjongLaunchMode.Battle;

            if (battleRoot != null)
                battleRoot.SetActive(isBattle);

            if (!isBattle)
                return;

            finished = false;
            battleActive = true;
            speedFactor = ResolveSpeedFactor(MahjongSession.BattleOpponentRankTier, MahjongSession.BattleOpponentRankPoints);

            currentScore = Random.Range(minStartScore, maxStartScore + 1);
            hiddenTempo = Random.Range(minTempo, maxTempo);

            ApplyVisuals(statusSearching);

            BindBoard();
            StartSimulation();
        }

        private void BindBoard()
        {
            if (board == null)
                return;

            board.WinTriggered -= HandlePlayerWin;
            board.LoseTriggered -= HandlePlayerLose;
            board.WinTriggered += HandlePlayerWin;
            board.LoseTriggered += HandlePlayerLose;
        }

        private void UnbindBoard()
        {
            if (board == null)
                return;

            board.WinTriggered -= HandlePlayerWin;
            board.LoseTriggered -= HandlePlayerLose;
        }

        private void StartSimulation()
        {
            StopSimulation();
            simulationRoutine = StartCoroutine(SimulateOpponent());
        }

        private void StopSimulation()
        {
            if (simulationRoutine != null)
            {
                StopCoroutine(simulationRoutine);
                simulationRoutine = null;
            }
        }

        private IEnumerator SimulateOpponent()
        {
            yield return Wait(RandomRange(minThinkDelay, maxThinkDelay));

            while (battleActive && !finished)
            {
                ApplyStatusByTempo();

                int burstCount = Random.Range(1, 4);
                for (int i = 0; i < burstCount && battleActive && !finished; i++)
                {
                    AddBurst();
                    yield return Wait(RandomRange(minBurstDelay, maxBurstDelay));
                }

                yield return Wait(RandomRange(minThinkDelay, maxThinkDelay));
            }
        }

        private void AddBurst()
        {
            int scoreGain = Mathf.RoundToInt(Random.Range(minBurstGain, maxBurstGain + 1) * speedFactor);
            float tempoGain = Random.Range(minBurstTempo, maxBurstTempo) * speedFactor;

            currentScore += Mathf.Max(1, scoreGain);
            hiddenTempo = Mathf.Clamp01(hiddenTempo + tempoGain);

            ApplyVisuals(GetStatusByTempo(hiddenTempo));
            PlayMoveFx(scoreGain);
        }

        private void HandlePlayerWin()
        {
            if (finished)
                return;

            finished = true;
            battleActive = false;
            StopSimulation();
            StartCoroutine(FinishAsPlayerWon());
        }

        private void HandlePlayerLose()
        {
            if (finished)
                return;

            finished = true;
            battleActive = false;
            StopSimulation();
            StartCoroutine(FinishAsPlayerLost());
        }

        private IEnumerator FinishAsPlayerWon()
        {
            float startTempo = hiddenTempo;
            float targetTempo = Mathf.Clamp01(Mathf.Min(0.97f, startTempo + 0.06f));
            float time = 0f;

            while (time < finishAnimTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / finishAnimTime);
                hiddenTempo = Mathf.Lerp(startTempo, targetTempo, t);
                ApplyVisuals(statusFinished);
                yield return null;
            }

            hiddenTempo = targetTempo;
            ApplyVisuals(statusFinished);
        }

        private IEnumerator FinishAsPlayerLost()
        {
            float startTempo = hiddenTempo;
            float targetTempo = 1f;
            int scoreBoost = Mathf.RoundToInt(Random.Range(120, 260) * speedFactor);
            int startScore = currentScore;
            int endScore = currentScore + scoreBoost;
            float time = 0f;

            while (time < finishAnimTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / finishAnimTime);
                hiddenTempo = Mathf.Lerp(startTempo, targetTempo, t);
                currentScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
                ApplyVisuals(statusFinished);
                yield return null;
            }

            hiddenTempo = 1f;
            currentScore = endScore;
            ApplyVisuals(statusFinished);
        }

        private void ApplyStatusByTempo()
        {
            ApplyVisuals(GetStatusByTempo(hiddenTempo));
        }

        private string GetStatusByTempo(float value)
        {
            if (value >= 0.72f)
                return statusPressure;

            if (value >= 0.18f)
                return statusPlaying;

            return statusSearching;
        }

        private void ApplyVisuals(string status)
        {
            if (opponentScoreText != null)
                opponentScoreText.text = scorePrefix + Mathf.Max(0, currentScore);

            if (opponentStatusText != null)
                opponentStatusText.text = status;
        }

        private void PlayMoveFx(int gain)
        {
            if (moveFxRoot == null || leftTileFx == null || rightTileFx == null || gainText == null)
                return;

            StopMoveFx();
            moveFxRoutine = StartCoroutine(PlayMoveFxRoutine(gain));
        }

        private void StopMoveFx()
        {
            if (moveFxRoutine != null)
            {
                StopCoroutine(moveFxRoutine);
                moveFxRoutine = null;
            }
        }

        private IEnumerator PlayMoveFxRoutine(int gain)
        {
            PrepareFxSprites();

            RectTransform leftRt = leftTileFx.rectTransform;
            RectTransform rightRt = rightTileFx.rectTransform;
            RectTransform gainRt = gainText.rectTransform;

            Vector2 leftStart = new Vector2(-tileOffsetX, 0f);
            Vector2 rightStart = new Vector2(tileOffsetX, 0f);
            Vector2 center = Vector2.zero;
            Vector2 gainStart = new Vector2(0f, 0f);
            Vector2 gainEnd = new Vector2(0f, gainRiseY);

            leftRt.anchoredPosition = leftStart;
            rightRt.anchoredPosition = rightStart;
            leftRt.localScale = Vector3.one;
            rightRt.localScale = Vector3.one;

            gainRt.anchoredPosition = gainStart;
            gainRt.localScale = Vector3.one;
            gainText.text = "+" + Mathf.Max(1, gain);

            SetImageAlpha(leftTileFx, 1f);
            SetImageAlpha(rightTileFx, 1f);
            SetTextAlpha(gainText, 1f);

            leftTileFx.gameObject.SetActive(true);
            rightTileFx.gameObject.SetActive(true);
            gainText.gameObject.SetActive(true);

            float moveTime = Mathf.Max(0.01f, tileMoveTime);
            float t = 0f;

            while (t < moveTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / moveTime);
                float eased = Mathf.SmoothStep(0f, 1f, k);

                leftRt.anchoredPosition = Vector2.LerpUnclamped(leftStart, center, eased);
                rightRt.anchoredPosition = Vector2.LerpUnclamped(rightStart, center, eased);
                gainRt.anchoredPosition = Vector2.LerpUnclamped(gainStart, gainEnd, eased);

                yield return null;
            }

            leftRt.anchoredPosition = center;
            rightRt.anchoredPosition = center;
            gainRt.anchoredPosition = gainEnd;

            float fadeTime = Mathf.Max(0.01f, tileFadeTime);
            float f = 0f;

            while (f < fadeTime)
            {
                f += Time.deltaTime;
                float k = Mathf.Clamp01(f / fadeTime);
                float alpha = 1f - k;

                SetImageAlpha(leftTileFx, alpha);
                SetImageAlpha(rightTileFx, alpha);
                SetTextAlpha(gainText, alpha);

                float scale = Mathf.Lerp(1f, 0.82f, k);
                leftRt.localScale = Vector3.one * scale;
                rightRt.localScale = Vector3.one * scale;
                gainRt.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, k);

                yield return null;
            }

            HideMoveFxImmediate();
            moveFxRoutine = null;
        }

        private void PrepareFxSprites()
        {
            Sprite sprite = ChooseFxSprite();

            if (leftTileFx != null)
                leftTileFx.sprite = sprite;

            if (rightTileFx != null)
                rightTileFx.sprite = sprite;
        }

        private Sprite ChooseFxSprite()
        {
            if (tileFxSprites != null && tileFxSprites.Length > 0)
            {
                int index = Random.Range(0, tileFxSprites.Length);
                if (tileFxSprites[index] != null)
                    return tileFxSprites[index];
            }

            return defaultTileSprite;
        }

        private void HideMoveFxImmediate()
        {
            if (leftTileFx != null)
                leftTileFx.gameObject.SetActive(false);

            if (rightTileFx != null)
                rightTileFx.gameObject.SetActive(false);

            if (gainText != null)
                gainText.gameObject.SetActive(false);
        }

        private void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text == null)
                return;

            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private float ResolveSpeedFactor(string rankTier, int rankPoints)
        {
            string tier = string.IsNullOrWhiteSpace(rankTier) ? string.Empty : rankTier.Trim().ToLowerInvariant();

            if (tier == "master")
                return masterSpeed;
            if (tier == "jade")
                return jadeSpeed;
            if (tier == "gold")
                return goldSpeed;
            if (tier == "silver")
                return silverSpeed;
            if (tier == "bronze")
                return bronzeSpeed;

            if (rankPoints >= 800)
                return masterSpeed;
            if (rankPoints >= 500)
                return jadeSpeed;
            if (rankPoints >= 250)
                return goldSpeed;
            if (rankPoints >= 100)
                return silverSpeed;

            return bronzeSpeed;
        }

        private float RandomRange(float min, float max)
        {
            if (max <= min)
                return min;

            return Random.Range(min, max);
        }

        private WaitForSeconds Wait(float time)
        {
            return new WaitForSeconds(Mathf.Max(0.01f, time));
        }
    }
}