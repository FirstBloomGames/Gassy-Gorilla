using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaWebBuild
    {
        private const string BuildFolderName = "Builds/WebGLPhone";
        private const string MobilePresentationMarker = "gg-orientation-gate";

        private const string BrowserQaBridge = @"                window.ggUnityInstance = unityInstance;
                var ggQaParameters = new URLSearchParams(window.location.search);
                var ggQaSmokeRun = ggQaParameters.has(""qa-smoke"");
                var ggQaCrocHitRun = ggQaParameters.has(""qa-croc-hit"");
                var ggQaCrocRun = ggQaParameters.has(""qa-croc"") || ggQaCrocHitRun;
                var ggQaAutomatedRun = ggQaSmokeRun || ggQaCrocRun;
                if (ggQaParameters.has(""qa-autoplay"") || ggQaAutomatedRun) {
                  window.setTimeout(() => unityInstance.SendMessage(""Manager_MainMenu"", ""Play""), 250);
                }
                if (ggQaCrocRun) {
                  window.setTimeout(() => {
                    unityInstance.SendMessage(""Director_RunChunks"", ""ConfigureSeedForQa"", ""6"");
                    unityInstance.SendMessage(""Manager_Game"", ""ConfigureCrocodileQa"", ggQaCrocHitRun ? ""hit"" : ""dodge"");
                  }, 3300);
                }
                if (ggQaAutomatedRun) {
                  window.setTimeout(() => {
                    var ggQaTapsRemaining = ggQaCrocHitRun ? 6 : 24;
                    var ggQaTap = () => {
                      var rect = canvas.getBoundingClientRect();
                      var x = rect.left + rect.width * 0.42;
                      var y = rect.top + rect.height * 0.52;
                      var down = new MouseEvent(""mousedown"", { bubbles: true, clientX: x, clientY: y, button: 0, buttons: 1 });
                      var up = new MouseEvent(""mouseup"", { bubbles: true, clientX: x, clientY: y, button: 0, buttons: 0 });
                      canvas.dispatchEvent(down);
                      canvas.dispatchEvent(up);
                      ggQaTapsRemaining--;
                      if (ggQaTapsRemaining > 0) window.setTimeout(ggQaTap, 1000);
                    };
                    ggQaTap();
                  }, 1650);
                }";

        private const string MobileOrientationMarkup = @"    <div id=""gg-orientation-gate"" aria-hidden=""true"">
      <div class=""gg-orientation-content"">
        <div class=""gg-orientation-brand"">GASSY GORILLA</div>
        <div class=""gg-phone-rotation"" aria-hidden=""true""><span></span></div>
        <h1>ROTATE TO PLAY</h1>
        <p>Turn your phone sideways for the full jungle.</p>
      </div>
    </div>";

        private const string MobileOrientationScript = @"      var ggForceMobilePreview = new URLSearchParams(window.location.search).has(""mobile-preview"");
      var ggIsMobileDevice = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || ggForceMobilePreview;
      var ggOrientationTimer = 0;

      function ggUpdateOrientationGate() {
        var portrait = window.innerHeight > window.innerWidth;
        var gateIsActive = ggIsMobileDevice && portrait;
        var gate = document.querySelector(""#gg-orientation-gate"");

        document.body.classList.toggle(""gg-portrait-active"", gateIsActive);
        if (gate) gate.setAttribute(""aria-hidden"", gateIsActive ? ""false"" : ""true"");
      }

      window.addEventListener(""resize"", ggUpdateOrientationGate);
      window.addEventListener(""orientationchange"", function() {
        window.clearTimeout(ggOrientationTimer);
        ggOrientationTimer = window.setTimeout(ggUpdateOrientationGate, 120);
      });
      ggUpdateOrientationGate();

";

        private const string MobilePresentationStyles = @"

/* Gassy Gorilla mobile orientation gate */
html, body {
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: #041b14;
}

#gg-orientation-gate {
  display: none;
  position: fixed;
  inset: 0;
  z-index: 10000;
  box-sizing: border-box;
  place-items: center;
  padding: max(24px, env(safe-area-inset-top)) max(24px, env(safe-area-inset-right)) max(24px, env(safe-area-inset-bottom)) max(24px, env(safe-area-inset-left));
  overflow: hidden;
  background: #063a2a;
  color: #ffffff;
  font-family: Arial, Helvetica, sans-serif;
  text-align: center;
}

body.gg-portrait-active #gg-orientation-gate {
  display: grid;
}

body.gg-portrait-active #unity-container {
  visibility: hidden;
}

.gg-orientation-content {
  display: grid;
  width: min(82vw, 340px);
  justify-items: center;
  gap: 14px;
}

.gg-orientation-brand {
  color: #70e6ef;
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0;
}

.gg-phone-rotation {
  position: relative;
  width: 52px;
  height: 86px;
  margin: 12px 0 16px;
  border: 4px solid #f9d85f;
  border-radius: 8px;
  box-sizing: border-box;
  animation: gg-rotate-phone 2.8s ease-in-out infinite alternate;
}

.gg-phone-rotation::before {
  content: """";
  position: absolute;
  top: 5px;
  left: 50%;
  width: 15px;
  height: 3px;
  border-radius: 2px;
  background: #f9d85f;
  transform: translateX(-50%);
}

.gg-phone-rotation span {
  position: absolute;
  inset: 13px 7px 8px;
  border: 2px solid #70e6ef;
  border-radius: 3px;
  background: #0b5a40;
}

.gg-orientation-content h1 {
  margin: 0;
  color: #f9d85f;
  font-size: 28px;
  line-height: 1.1;
  letter-spacing: 0;
}

.gg-orientation-content p {
  margin: 0;
  max-width: 280px;
  color: #ffffff;
  font-size: 17px;
  line-height: 1.45;
  letter-spacing: 0;
}

@keyframes gg-rotate-phone {
  0%, 18% { transform: rotate(0deg); }
  72%, 100% { transform: rotate(90deg); }
}

@media (prefers-reduced-motion: reduce) {
  .gg-phone-rotation {
    animation: none;
    transform: rotate(90deg);
  }
}

@media (max-width: 959px), (max-height: 641px) {
  #unity-container.unity-desktop {
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
    transform: none;
  }

  #unity-container.unity-desktop #unity-canvas {
    width: 100% !important;
    height: 100% !important;
  }

  #unity-container.unity-desktop #unity-footer {
    display: none;
  }
}
";

        private const string WebManifest = @"{
  ""name"": ""Gassy Gorilla"",
  ""short_name"": ""Gassy Gorilla"",
  ""start_url"": ""./"",
  ""scope"": ""./"",
  ""display"": ""fullscreen"",
  ""orientation"": ""landscape"",
  ""background_color"": ""#041b14"",
  ""theme_color"": ""#063a2a""
}";

        [MenuItem("First Bloom/Gassy Gorilla/Build Phone Web Preview")]
        public static void BuildPhonePreview()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("Gassy Gorilla has no enabled scenes in Build Settings.");
            }

            GassyGorillaSceneSimplifier.ApplySceneCleanup();
            GassyGorillaWebAssetOptimizer.ApplyWebGlImportSettings();
            GassyGorillaWebAssetOptimizer.ValidateBuildDependencies();
            GassyGorillaParticleRepair.RepairProjectAssets();
            GassyGorillaPerformanceAudit.ValidateRuntimeGeometryBudgets();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectRoot, BuildFolderName);
            Directory.CreateDirectory(outputPath);

            PlayerSettings.companyName = "First Bloom Games";
            PlayerSettings.productName = "Gassy Gorilla";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Gassy Gorilla phone preview build failed with result " + report.summary.result + ".");
            }

            OptimizeGeneratedPageForPhone(outputPath);
            Debug.Log(
                "Gassy Gorilla phone preview built at " + outputPath
                + " (" + report.summary.totalSize + " bytes)."
            );
        }

        private static void OptimizeGeneratedPageForPhone(string outputPath)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException("Unity did not generate the phone preview index page.", indexPath);
            }

            string html = File.ReadAllText(indexPath);
            html = Regex.Replace(
                html,
                "<title>.*?</title>",
                "<title>Gassy Gorilla | First Bloom Games</title>",
                RegexOptions.Singleline
            );
            html = ReplaceRequired(
                html,
                "    <link rel=\"shortcut icon\"",
                "    <meta name=\"theme-color\" content=\"#063a2a\">" + Environment.NewLine
                + "    <meta name=\"apple-mobile-web-app-capable\" content=\"yes\">" + Environment.NewLine
                + "    <meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">" + Environment.NewLine
                + "    <meta name=\"apple-mobile-web-app-title\" content=\"Gassy Gorilla\">" + Environment.NewLine
                + "    <link rel=\"manifest\" href=\"manifest.webmanifest\">" + Environment.NewLine
                + "    <link rel=\"shortcut icon\"",
                "web app metadata"
            );
            html = ReplaceRequired(
                html,
                "  <body>",
                "  <body style=\"overscroll-behavior:none; touch-action:none;\">" + Environment.NewLine
                + MobileOrientationMarkup,
                "mobile body presentation"
            );
            html = ReplaceRequired(
                html,
                "      var canvas = document.querySelector(\"#unity-canvas\");",
                MobileOrientationScript
                + "      var canvas = document.querySelector(\"#unity-canvas\");",
                "mobile orientation script"
            );
            html = ReplaceRequired(
                html,
                "      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
                "      if (ggIsMobileDevice) {",
                "mobile device detection"
            );
            html = ReplaceRequired(
                html,
                "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";",
                "      ggUpdateOrientationGate();" + Environment.NewLine + Environment.NewLine
                + "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";",
                "orientation refresh after mobile viewport setup"
            );
            html = ReplaceRequired(
                html,
                "// config.devicePixelRatio = 1;",
                "config.devicePixelRatio = 1;",
                "mobile device pixel ratio"
            );
            html = ReplaceRequired(
                html,
                "      // config.autoSyncPersistentDataPath = true;",
                "      config.autoSyncPersistentDataPath = true;" + Environment.NewLine
                + "      config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.25);",
                "persistent data autosync"
            );
            html = ReplaceRequired(
                html,
                "              }).then((unityInstance) => {",
                "              }).then((unityInstance) => {" + Environment.NewLine
                + BrowserQaBridge,
                "browser QA bridge"
            );
            File.WriteAllText(indexPath, NormalizeLineEndings(html));

            string stylePath = Path.Combine(outputPath, "TemplateData", "style.css");
            if (!File.Exists(stylePath))
            {
                throw new FileNotFoundException("Unity did not generate the WebGL template stylesheet.", stylePath);
            }

            string css = File.ReadAllText(stylePath);
            if (!css.Contains(MobilePresentationMarker))
            {
                css += MobilePresentationStyles;
            }

            File.WriteAllText(stylePath, NormalizeLineEndings(css));
            File.WriteAllText(
                Path.Combine(outputPath, "manifest.webmanifest"),
                NormalizeLineEndings(WebManifest) + "\n"
            );
        }

        private static string ReplaceRequired(
            string content,
            string expected,
            string replacement,
            string description
        )
        {
            if (!content.Contains(expected))
            {
                throw new InvalidOperationException(
                    "Unity's generated WebGL page no longer contains the expected " + description + " marker."
                );
            }

            return content.Replace(expected, replacement);
        }

        private static string NormalizeLineEndings(string content)
        {
            return content.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
