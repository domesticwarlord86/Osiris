using System;
using System.ComponentModel;
using System.IO;
using Clio.Utilities;
using ff14bot.Enums;
using ff14bot.Helpers;

namespace OsirisPlugin
{
    public class OsirisSettings : JsonSettings
    {
        private static OsirisSettings _settings;
        public static OsirisSettings Instance => _settings ?? (_settings = new OsirisSettings());

        public OsirisSettings() : base(Path.Combine(CharacterSettingsDirectory, "OsirisSettings.json")) {

        }
 
        private bool _waitForRelease;
        [Description("Should we wait until the release timer runs out, or release immediatly?" +
                     "\n Setting this to true will cause the bot to wait the 10 minute timer until either we get a raise or it runs out." +
                     "\n Setting this to false will immediatly release once the party is out of combat.")]
        [Category("Misc")]
        [DefaultValue(false)]
        public bool ReleaseWait
        {
            get => _waitForRelease;
            set
            {
                if (_waitForRelease != value)
                {
                    _waitForRelease = value;
                    Save();
                }
            }
        }
        
        private bool _raiseshout;
        [Description("Shout for raises while in Bozja or Eureka." +
                     "\n Setting this to true will cause the bot to shout for raise and wait until the timer runs out or we get a raise." +
                     "\n Be careful while using this, as it could cause spam in the chat.")]
        [Category("Bozja/Eureka")]
        [DefaultValue(false)]
        public bool RaiseShout
        {
            get => _raiseshout;
            set
            {
                if (_raiseshout != value)
                {
                    _raiseshout = value;
                    Save();
                }
            }
        }
        
        private bool _thankRaiser;
        [Description("Thank the raiser in say after being raised by someone.")]
        [Category("Bozja/Eureka")]
        [DefaultValue(true)]
        public bool ThankYouSir
        {
            get => _thankRaiser;
            set
            {
                if (_thankRaiser != value)
                {
                    _thankRaiser = value;
                    Save();
                }
            }
        }
        
        private int _shoutTime;
        [Description("Time to wait in minutes between shouts for raise.")]
        [DefaultValue(5)]
        [Category("Bozja/Eureka")]
        public int ShoutTime
        {
            get => _shoutTime;
            set
            {
                if (_shoutTime != value)
                {
                    _shoutTime = value;
                    Save();
                }
            }
        }        

    }
}
