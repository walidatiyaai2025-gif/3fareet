using UnityEngine;

namespace Afareet.UI
{
    public sealed class UnitySplashOverlay : MonoBehaviour
    {
        private const float Duration = 5f;
        private Texture2D artwork;
        private Texture2D purple;
        private Texture2D gold;
        private GUIStyle loadingStyle;
        private float startedAt;

        private void Awake()
        {
            artwork = Resources.Load<Texture2D>("afareet_splash_landscape");
            purple = Solid(new Color(.57f, .08f, .9f, 1f));
            gold = Solid(new Color(1f, .66f, .12f, 1f));
            startedAt = Time.realtimeSinceStartup;
            Time.timeScale = 0f;
        }

        private void OnGUI()
        {
            var elapsed = Time.realtimeSinceStartup - startedAt;
            var progress = Mathf.Clamp01(elapsed / Duration);
            if (progress >= 1f)
            {
                Time.timeScale = 1f;
                Destroy(this);
                return;
            }

            GUI.depth = -1000;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            if (artwork != null)
            {
                var sourceAspect = artwork.width / (float)artwork.height;
                var screenAspect = Screen.width / (float)Screen.height;
                Rect destination;
                if (screenAspect > sourceAspect)
                {
                    var width = Screen.height * sourceAspect;
                    destination = new Rect((Screen.width - width) * .5f, 0f, width, Screen.height);
                }
                else
                {
                    var height = Screen.width / sourceAspect;
                    destination = new Rect(0f, (Screen.height - height) * .5f, Screen.width, height);
                }
                GUI.DrawTexture(destination, artwork, ScaleMode.StretchToFill);
            }
            else GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), purple);

            var barWidth = Mathf.Min(Screen.width * .62f, 540f);
            var bar = new Rect((Screen.width - barWidth) * .5f, Screen.height - 42f, barWidth, 14f);
            GUI.DrawTexture(new Rect(bar.x - 3f, bar.y - 3f, bar.width + 6f, bar.height + 6f), gold);
            GUI.DrawTexture(bar, Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * progress, bar.height), purple);
            loadingStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 34, 16, 28),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0f, bar.y - 38f, Screen.width, 30f), $"LOADING  {Mathf.RoundToInt(progress * 100f)}%", loadingStyle);
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy() => Time.timeScale = 1f;
    }
}
