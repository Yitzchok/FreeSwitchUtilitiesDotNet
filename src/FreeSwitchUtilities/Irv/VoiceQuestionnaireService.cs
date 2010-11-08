using System;
using System.IO;
using FreeSWITCH.Native;

namespace FreeSwitchUtilities.Irv
{
    public class VoiceQuestionnaireService
    {
        public ManagedSession Session { get; set; }
        public string InvalidAudioFile { get; set; }
        public string PhraseStart { get; set; }
        public string RecordingFolder { get; set; }

        public VoiceQuestionnaireService(ManagedSession session,
            string invalidAudioFile, string phraseStart, string recordingFolder)
        {
            Session = session;
            InvalidAudioFile = invalidAudioFile;
            PhraseStart = phraseStart;
            RecordingFolder = recordingFolder;
        }

        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, Func<string, string> isCorrectQuestion, string regexPattern,
                                           Func<string, bool> isValid)
        {
            return AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators, question, InvalidAudioFile, isCorrectQuestion,
                                 regexPattern, isValid);
        }


        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string invalidInput, Func<string, string> isCorrectQuestion, string regexPattern,
                                           Func<string, bool> isValid)
        {

            CheckSessionReady();
            string result = PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators, question,
                                             invalidInput, regexPattern);

            if (isValid(result))
            {
                var isCorrectInput = AskToVerifyAnswer(invalidInput, result, isCorrectQuestion);

                if (!isCorrectInput)
                {
                    CheckSessionReady();
                    result = AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators,
                                                  question, invalidInput, isCorrectQuestion, regexPattern, isValid);
                }
            }
            else
            {
                CheckSessionReady();
                Session.StreamFile(invalidInput, 0);
                CheckSessionReady();
                result = AskAndVerifyQuestion(minDigits, maxDigits, tries - 1, timeout, terminators,
                                              question, isCorrectQuestion, regexPattern, isValid);
            }

            return result;
        }

        private bool AskToVerifyAnswer(string invalidInput, string result, Func<string, string> isCorrectQuestion)
        {
            CheckSessionReady();
            var isCorrectInput = AskAndVerifyQuestion(1, 1, 3, 8000, "#",
                                                      isCorrectQuestion(result), invalidInput, "[12]") == "1";
            return isCorrectInput;

        }

        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string regexPattern)
        {
            return AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators, question, InvalidAudioFile,
                                        regexPattern);
        }

        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string invalidInput, string regexPattern)
        {
            CheckSessionReady();

            string result = PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators, question,
                                             invalidInput, regexPattern);

            return result;
        }

        public string PlayAndGetDigits(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                       string audioFile, string badInputAudioFile, string regexPattern)
        {
            CheckSessionReady();

            return Session.PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators,
                                            PhraseStart + audioFile,
                                            badInputAudioFile, regexPattern, String.Empty, 5000);
        }

        private bool CheckIfSessionReady()
        {
            return Session.Ready();
        }

        private void CheckSessionReady()
        {
            if (!Session.Ready()) throw new Exception();
        }

        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, 60);
        }

        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName, int timeLimit)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, timeLimit, 2);
        }

        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName, int timeLimit, int silenceHits)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, timeLimit, 500, silenceHits);
        }

        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion,
            string recordFileName, int timeLimit, int silenceThreshold, int silenceHits)
        {
            var fileNameCombined = Path.Combine(RecordingFolder, recordFileName) + ".wav";

            CheckSessionReady();
            Session.StreamFile(PhraseStart + question, 0);

            RecordToFile(fileNameCombined, timeLimit, silenceThreshold, silenceHits);

            CheckSessionReady();
            var isRecordingFine = AskToVerifyAnswer("invalid", fileNameCombined, isCorrectQuestion);

            if (!isRecordingFine)
            {
                fileNameCombined = AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, timeLimit, silenceThreshold,
                                           silenceHits);
            }

            return fileNameCombined;
        }

        private void RecordToFile(string fileName, int timeLimit, int silenceThreshold, int silenceSeconds)
        {
            Session.DtmfReceivedFunction = (c, t) => c == '#' ? "false" : "true";

            CheckSessionReady();
            Session.RecordFile(fileName, timeLimit, silenceThreshold, silenceSeconds);
            CheckSessionReady();

            Session.DtmfReceivedFunction = null;
        }
    }
}