using System;
using System.IO.Pipes;
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

namespace LlamaLibrary
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
        
        private static NamedPipeClientStream pipe;

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


        private async Task<bool> HandleDeath()
        {
            if (DutyManager.InInstance)
            {
                Log("Handling Death");
                
                //Logic for Bozja and Eureka
                if (WorldManager.ZoneId == 975 || WorldManager.ZoneId == 920 || WorldManager.ZoneId == 732 || WorldManager.ZoneId == 763 || WorldManager.ZoneId == 795 || WorldManager.ZoneId == 827)
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
                                        break;
                                    }
                                }

                                await Coroutine.Sleep(500);
                        }
                        else
                        {
                            Log("Shout for raise not selected. Waiting for raise or auto Starting Point.");
                            await Coroutine.Wait(-1, () => Core.Me.HasAura(148) || Core.Me.CurrentHealth > 1);
                        }
                    }
                    else
                    {
                        Log("In party, waiting for party member to raise us.");
                        await Coroutine.Wait(-1, () => Core.Me.HasAura(148) || Core.Me.CurrentHealth > 1);                        
                    }

                    Log($"We have Raise Aura");
                    if (NotificationRevive.IsOpen)
                    {
                        Log($"Clicking Accept");
                        ClientGameUiRevive.Revive();
                        await Coroutine.Wait(-1, () => (CommonBehaviors.IsLoading));
                        Log($"Waiting for loading to finish...");
                        await Coroutine.Wait(-1, () => (!CommonBehaviors.IsLoading));
                        if (OsirisSettings.Instance.RaiseShout)
                        {
                            ChatManager.SendChat("/say Thanks for the raise.");
                        }
                    }

                    await Coroutine.Wait(15000, () => ClientGameUiRevive.ReviveState == ReviveState.None);

                    Poi.Clear("We live!");
                    return true;                    
                }
                
                while (PartyManager.AllMembers.Any(i=> i.GetType() == typeof(TrustPartyMember)))
								{
									Log($"In a NPC party.");
									await Coroutine.Wait(-1, () => (Core.Me.IsAlive));  
									Log($"We are alive, loading profile...");
									NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path, true);
									NeoProfileManager.UpdateCurrentProfileBehavior();
									await Coroutine.Sleep(5000);
									return true;								
                }                
								
                while (PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat))
                {
									// Check if loading in the case of instant release like Squadron dungeons	
									if (CommonBehaviors.IsLoading)
									{
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
											Log($"We are alive, loading profile...");
											NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path, true);
											NeoProfileManager.UpdateCurrentProfileBehavior();
											await Coroutine.Sleep(5000);
											return true;
									}										
                    await Coroutine.Wait(3000, () => ClientGameUiRevive.ReviveState == ReviveState.Dead);
                    Log("Party memebers in combat, waiting for Raise.");
                    await Coroutine.Wait(-1, () => Core.Me.HasAura(148) || !PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat));
                    await Coroutine.Sleep(500);
                    if (!PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat))
                    {
                        break;
                    }
                    Log($"We have Raise Aura");
                    if (NotificationRevive.IsOpen)
                    {
                        Log($"Clicking Accept");
                        ClientGameUiRevive.Revive();
                    }

                    await Coroutine.Wait(15000, () => ClientGameUiRevive.ReviveState == ReviveState.None);

                    Poi.Clear("We live!");
                    return true;
                }

                while (!PartyManager.AllMembers.Any(i => i.BattleCharacter.InCombat))
                {
									// Check if loading in the case of instant release like Squadron dungeons	
									if (CommonBehaviors.IsLoading)
									{
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
											Log($"We are alive, loading profile...");
											NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path, true);
											NeoProfileManager.UpdateCurrentProfileBehavior();
											await Coroutine.Sleep(5000);
											return true;
									}										
                    await Coroutine.Wait(3000, () => ClientGameUiRevive.ReviveState == ReviveState.Dead);
                    Log("No one is in combat, releasing...");
                    await Coroutine.Sleep(500);
                    ff14bot.RemoteWindows.SelectYesno.ClickYes();
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
                    Log($"We are alive, loading profile...");
                    NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path, true);
                    NeoProfileManager.UpdateCurrentProfileBehavior();
                    await Coroutine.Sleep(5000);
                    return true;
                }
								
                
            }
            
            if (!DutyManager.InInstance && !PartyManager.IsInParty)
            {
                Log($"Dead and not in party. Accepting return home and restarting profile.");
                await Coroutine.Sleep(5000);
                ff14bot.RemoteWindows.SelectYesno.ClickYes();
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
                Log($"We are alive, loading profile...");
                NeoProfileManager.Load(NeoProfileManager.CurrentProfile.Path, true);
                NeoProfileManager.UpdateCurrentProfileBehavior();
                await Coroutine.Sleep(5000);
                return true;
            }            

            return false;
        }


        private static void Log(string text)
        {
            var msg = string.Format($"[{name}] " + text);
            Logging.Write(Colors.Bisque, msg);
        }
    }
}