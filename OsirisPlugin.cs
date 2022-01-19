using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.RemoteWindows;
using TreeSharp;

namespace OsirisPlugin
{
    public class OsirisPlugin : BotPlugin
    {
        private static readonly string name = "Osiris";
        
        private static string[] Shouts = new string[]
        {
            "/shout Raise please at <pos>.",
            "/shout Could I please get a raise at <pos>.",
            "/shout Ugh, dead again, anyone available at <pos>.",
            "/shout Dead body at <pos>, can anyone help.",
            "/shout Code 187 found in crater at <pos> suspect fled the scene described as red feathered and yelling 'Wark'",
            "/shout Sprout asks me what happened when HP hits zero. Well, I demonstrated it and need assistance at <pos>",
            "/shout Anyone have a Mettle Detector? I seem to have lost some of mine at <pos> and need help",
            "/shout Nike and Martha Stewart worked me too hard at the sweatshop today and I finally fell over. Can someone give me a boost at <pos>",
            "/shout Well this is embarrassing , raise please <pos>."
        };        
        
        private static readonly Random _random = new Random();

        private Composite deathCoroutine;
        private OsirisSettingsForm _form;
        public override string Author { get; } = "Kayla, DomesticWarlord86";

        public override Version Version => new Version(0, 1);

        public override string Name { get; } = name;

        public override bool WantButton => true;

        public override void OnButtonPress()
        {
            if (_form == null)
            {
                _form = new OsirisSettingsForm()
                {
                    Text = "Osiris Settings v" + Version,
                };
                _form.Closed += (o, e) => { _form = null; };
            }

            try
            {
                _form.Show();
            }
            catch (Exception e)
            {
                // ignored
            }   
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
        
        private bool IsInBozjaOrEureka()
        {
            return WorldManager.ZoneId == 975 || WorldManager.ZoneId == 920 || WorldManager.ZoneId == 732 ||
                   WorldManager.ZoneId == 763 || WorldManager.ZoneId == 795 || WorldManager.ZoneId == 827;
        }

        private bool HasRezzerInParty()
        {
            return PartyManager.AllMembers.Any(i => i.BattleCharacter.CurrentJob == ClassJobType.WhiteMage || 
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Scholar ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Summoner ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.RedMage ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Astrologian ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Sage ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Arcanist ||
                                                    i.BattleCharacter.CurrentJob == ClassJobType.Conjurer);
        }
        
        private bool AnyPartyMemberAlive()
        {
            return PartyManager.AllMembers.Any(i => i.BattleCharacter.IsAlive);
        }


        private async Task<bool> HandleDeath()
        {
            var currentZone = WorldManager.CurrentZoneName;
            
            // Check if loading in the case of instant release like Squadron dungeons	
            if (CommonBehaviors.IsLoading)
            {
                Log($"Waiting to be alive.");
                await Coroutine.Wait(-1, () => (Core.Me.IsAlive));
            }
            
 
            //Logic for Bozja and Eureka
            if (IsInBozjaOrEureka())
            {
                Log($"Handling Death For {currentZone}");
                await HandleDeathInBozjaAndEureka();
            }
               
            // Logic for NPC parties
            while (PartyManager.AllMembers.Any(i=> i.GetType() == typeof(TrustPartyMember)))
            {
                Log($"In a NPC party. Waiting for zone.");
                await Coroutine.Wait(-1, () => (Core.Me.IsAlive));
            }                
								
            //Logic for In Duty
            if (DutyManager.InInstance && !IsInBozjaOrEureka())
            {
                await HandleDeathInInstance();
            }

            //Logic for not in a party
            if (PartyManager.IsInParty && !IsInBozjaOrEureka() && !DutyManager.InInstance)
            {
                if (HasRezzerInParty())
                {
                    Log($"We died in {currentZone}. We have a Raiser.");
                    await WaitForLife();
                }
                else
                {
                    if (OsirisSettings.Instance.ReleaseWait)
                    {
                        Log($"We died in {currentZone}. Wait for release checked.");
                        await WaitForLife();
                    }
                    else
                    {
                        Log($"We died in {currentZone} with no Raiser in party.");
                        await Release();
                    }
                }
            }
                
            //Logic for everything else
            if (!PartyManager.IsInParty && !IsInBozjaOrEureka() && !DutyManager.InInstance)
            {

                if (OsirisSettings.Instance.ReleaseWait)
                {
                    await WaitForLife();
                }
                else
                {
                    await Release();
                }

            }           
            

            Log($"We are alive, loading profile...");
            NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path);
            NeoProfileManager.UpdateCurrentProfileBehavior();
            await Coroutine.Sleep(5000);
            return true;
        }

        private async Task HandleDeathInBozjaAndEureka()
        { 
            await Coroutine.Wait(3000, () => ClientGameUiRevive.ReviveState == ReviveState.Dead);
            if (!PartyManager.IsInParty)
            {
                if (OsirisSettings.Instance.RaiseShout)
                {
                    while (!Core.Me.HasAura(148) && Core.Me.CurrentHealth < 1)
                    {
                        Log("Shout for raise selected, shouting...");
                        ChatManager.SendChat(Shouts[_random.Next(0, Shouts.Length)]);
                        await Coroutine.Wait(OsirisSettings.Instance.ShoutTime * 60 * 1000, () => Core.Me.HasAura(148) || Core.Me.CurrentHealth > 1);
                        if (Core.Me.HasAura(148))
                        {
                            await AcceptRaise();
                            break;
                        }
                        if (Core.Me.CurrentHealth > 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (OsirisSettings.Instance.ReleaseWait)
                    {
                        await WaitForLife();
                    }
                    else
                    {
                        Log("Releasing in Bozja, gonna loose some mettle here.");
                        await Release();

                    }
                }
                    
            }
            // If is in party
            else
            {
                if (OsirisSettings.Instance.ReleaseWait)
                {
                    if (HasRezzerInParty())
                    {
                        Log("In a party with a rezzer, waiting for party member to raise us.");
                        await WaitForLife();
                    }
                    else
                    {
                        if (OsirisSettings.Instance.RaiseShout)
                        {
                            while (!Core.Me.HasAura(148) && Core.Me.CurrentHealth < 1)
                            {
                                Log("In a party with no rezzer. Shout for raise selected, shouting...");
                                ChatManager.SendChat(Shouts[_random.Next(0, Shouts.Length)]);
                                await Coroutine.Wait(OsirisSettings.Instance.ShoutTime * 60 * 1000, () => Core.Me.HasAura(148) || Core.Me.CurrentHealth > 1);
                                if (Core.Me.HasAura(148))
                                {
                                    await AcceptRaise();
                                    break;
                                }
                                if (Core.Me.CurrentHealth > 1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (OsirisSettings.Instance.ReleaseWait)
                            {
                                    Log("In party, with no rezzer but ReleaseWait is checked. Waiting for rez.");
                                    await WaitForLife();                        

                            }
                            else
                            {
                                Log("In a party without a Rezzer. Releasing in Bozja, gonna loose some mettle here.");
                                await Release();
                            }
                        }
                    }
                }

            }

            if (NotificationRevive.IsOpen)
            {
                await AcceptRaise();
            }
        }

        private async Task HandleDeathInInstance()
        {
            if (PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat) && AnyPartyMemberAlive())
            {
                Log("Party memebers in combat, waiting for Raise.");
                await Coroutine.Wait(3000, () => ClientGameUiRevive.ReviveState == ReviveState.Dead);
                await Coroutine.Wait(-1, () => Core.Me.HasAura(148) || !PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat) && AnyPartyMemberAlive());
                await Coroutine.Sleep(500);
                if (Core.Me.HasAura(148))
                {
                    await AcceptRaise();
                    return;
                }
            }

            await Release();
        }

        private async Task Release()
        {
            Log($"Releasing.");
            await Coroutine.Wait(-1, () => (SelectYesno.IsOpen));
            SelectYesno.ClickYes();
            await Coroutine.Wait(-1, () => (CommonBehaviors.IsLoading));
            while (CommonBehaviors.IsLoading)
            {
                Log($"Waiting for zoning to finish...");
                await Coroutine.Wait(-1, () => (!CommonBehaviors.IsLoading));
            }            
            while (!Core.Me.IsAlive)
            {
                Log($"Zoning finsihed, waiting to become alive...");
                await Coroutine.Wait(-1, () => (Core.Me.IsAlive));
            }

        }

        private async Task AcceptRaise()
        {
            Log($"We have Raise Aura.");
            Log($"Clicking Accept");
            await Coroutine.Sleep(500);
            ClientGameUiRevive.Revive();
            await Coroutine.Wait(-1, () => (CommonBehaviors.IsLoading));
            Log($"Waiting for loading to finish...");
            await Coroutine.Wait(-1, () => (Core.Me.IsAlive));
            if (OsirisSettings.Instance.ThankYouSir)
            {
                ChatManager.SendChat("/say Thanks for the raise.");
            }
            await Coroutine.Wait(15000, () => ClientGameUiRevive.ReviveState == ReviveState.None);
            Poi.Clear("We live!");
        }

        private async Task WaitForLife()
        {
            Log("Waiting for raise or auto Starting Point.");
            await Coroutine.Wait(-1, () => Core.Me.HasAura(148) || Core.Me.CurrentHealth > 1);
            if (Core.Me.HasAura(148))
            {
                await AcceptRaise();
            }

        }


        private static void Log(string text)
        {
            var msg = string.Format($"[{name}] " + text);
            Logging.Write(Colors.Bisque, msg);
        }
    }
}