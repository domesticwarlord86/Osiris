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
        
        private bool _raiseshout;

        [Description("Shout for raises while in Bozja or Eureka.")]
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
        
        private int _shoutTime;
        
        [Description("Time to wait between shouts for raise.")]
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