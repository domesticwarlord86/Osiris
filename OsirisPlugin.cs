using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.RemoteWindows;
using TreeSharp;

namespace LlamaLibrary
{
    public class OsirisPlugin : BotPlugin
    {
        private static readonly string name = "Osiris";

        private static NamedPipeClientStream pipe;

        private Composite deathCoroutine;
        public override string Author { get; } = "Kayla";

        public override Version Version => new Version(0, 1);

        public override string Name { get; } = name;

        public override bool WantButton => true;

        public override void OnButtonPress()
        {
        }

        public override void OnInitialize()
        {
            deathCoroutine = new ActionRunCoroutine(ctx => HandleDeath());
        }

        public override void OnEnabled()
        {
            TreeRoot.OnStart += setHooks;
        }

        private void setHooks(BotBase bot)
        {
            Log("Setting Hooks");
            TreeHooks.Instance.AddHook("DeathReviveLogic", deathCoroutine);
        }

        public override void OnDisabled()
        {
            TreeRoot.OnStart -= setHooks;
        }


        private async Task<bool> HandleDeath()
        {
            Log("Handling Death");

            await Coroutine.Wait(3000, () => ClientGameUiRevive.ReviveState == ReviveState.Dead);


            await Coroutine.Wait(-1, () => Core.Me.HasAura(148));
            await Coroutine.Sleep(500);
            Log($"We have Raise Aura");
            if (NotificationRevive.IsOpen)
            {
                Log($"Clicking Accept");
                ClientGameUiRevive.Revive();
            }

            await Coroutine.Wait(15000, () => ClientGameUiRevive.ReviveState == ReviveState.None);

            Poi.Clear("We live!");

            return false;
        }


        private static void Log(string text)
        {
            var msg = string.Format($"[{name}] " + text);
            Logging.Write(Colors.Bisque, msg);
        }
    }
}