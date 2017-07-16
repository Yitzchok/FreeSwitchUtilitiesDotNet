using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreeSWITCH;
using FreeSWITCH.Native;
using FreeSwitchUtilities.Irv;
using AppContext = FreeSWITCH.AppContext;

namespace FreeSwitchUtilities.Samples
{
    public class VoiceQuestionnaireServiceSample : IAppPlugin
    {
        public ManagedSession Session { get; set; }
        private const string PhraseStart = "phrase:sample_ivr_";
        private const string InvalidAudioFile = PhraseStart + "invalid";
        private const string RecordLocation = "/usr/local/freeswitch/recordings/";

        public void Run(AppContext context)
        {
            Session = context.Session;
            Session.Answer();
            Session.SetTtsParameters("flite", "kal");
            Session.sleep(1000, 0);

            var questionnaireService = new VoiceQuestionnaireService(
                                            Session, InvalidAudioFile,
                                            PhraseStart, RecordLocation);

            var phoneNumber = questionnaireService
                .AskAndVerifyQuestion(
                9, 11, 3, 10000, "#", "enter_phone_number",
                x => "you_entered_phone_number:" + x, "\\d+", _ => true);

            var nameRecordingFile = questionnaireService
                    .AskRecordAndVerifyQuestion(
                    "record_name", x => "verify_recording:" + x,
                    string.Format("{0}_{1}_{2}", Session.Uuid, DateTime.UtcNow.ToString("MMddyyyy"), phoneNumber));

        }
    }
}
