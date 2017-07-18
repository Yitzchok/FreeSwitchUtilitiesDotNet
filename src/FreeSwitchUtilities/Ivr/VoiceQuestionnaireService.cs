using System;
using System.IO;
using System.Threading;
using FreeSWITCH.Native;

namespace FreeSwitchUtilities.Ivr
{
    public class VoiceQuestionnaireService
    {
        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        /// <value>The session.</value>
        public ManagedSession Session { get; set; }

        /// <summary>
        /// Gets or sets the invalid audio file.
        /// </summary>
        /// <value>The invalid audio file.</value>
        public string InvalidAudioFile { get; set; }

        /// <summary>
        /// Gets or sets the phrase start.
        /// </summary>
        /// <value>The phrase start.</value>
        public string PhraseStart { get; set; }

        /// <summary>
        /// Gets or sets the recording folder.
        /// </summary>
        /// <value>The recording folder.</value>
        public string RecordingFolder { get; set; }

        public int WaitBeforeTryingToVerify { get; set; }

        public int DefaultDigitTimeout { get; set; } = 5000;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceQuestionnaireService"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="invalidAudioFile">The invalid audio file.</param>
        /// <param name="phraseStart">The phrase start.</param>
        /// <param name="recordingFolder">The recording folder.</param>
        public VoiceQuestionnaireService(ManagedSession session,
            string invalidAudioFile, string phraseStart, string recordingFolder)
        {
            Session = session;
            InvalidAudioFile = invalidAudioFile;
            PhraseStart = phraseStart;
            RecordingFolder = recordingFolder;
        }

        /// <summary>
        /// Asks the and verify question.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="question">The question.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <param name="isValid">The is valid.</param>
        /// <returns></returns>
        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, Func<string, string> isCorrectQuestion, string regexPattern,
                                           Func<string, IsValidResult> isValid)
        {
            return AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators, question, InvalidAudioFile, isCorrectQuestion,
                                 regexPattern, isValid);
        }


        /// <summary>
        /// Asks the and verify question.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="question">The question.</param>
        /// <param name="invalidInput">The invalid input.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <param name="isValid">The is valid.</param>
        /// <returns></returns>
        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string invalidInput, Func<string, string> isCorrectQuestion, string regexPattern,
                                           Func<string, IsValidResult> isValid)
        {
            CheckSessionReady();
            string result = PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators, question,
                                             invalidInput, regexPattern);

            if (WaitBeforeTryingToVerify > 0)
                Session.sleep(WaitBeforeTryingToVerify, 0);

            var isValidResult = isValid(result);

            if (isValidResult.IsValid)
            {
                var isCorrectQuestionToAsk = isCorrectQuestion(result);
                if (!string.IsNullOrEmpty(isCorrectQuestionToAsk))
                {
                    var isCorrectInput = AskToVerifyAnswer(invalidInput, isCorrectQuestionToAsk);

                    if (!isCorrectInput)
                    {
                        CheckSessionReady();
                        result = AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators,
                                                      question, invalidInput, isCorrectQuestion, regexPattern, isValid);
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(isValidResult.OverrideInput))
                {
                    if (!string.IsNullOrEmpty(invalidInput))
                    {
                        CheckSessionReady();
                        Session.StreamFile(invalidInput, 0);
                    }
                }
                else
                {
                    CheckSessionReady();
                    Session.StreamFile(isValidResult.OverrideInput, 0);
                }

                CheckSessionReady();
                result = AskAndVerifyQuestion(minDigits, maxDigits, tries - 1, timeout, terminators,
                                              question, invalidInput, isCorrectQuestion, regexPattern, isValid);
            }

            return result;
        }

        /// <summary>
        /// Asks to verify answer.
        /// </summary>
        /// <param name="invalidInput">The invalid input.</param>
        /// <param name="result">The result.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <returns></returns>
        private bool AskToVerifyAnswer(string invalidInput, string isCorrectQuestion)
        {
            CheckSessionReady();

            try
            {
                return AskAndVerifyQuestion(1, 1, 3, 8000, "#",
                                         isCorrectQuestion, invalidInput, "[12]") == "1";
            }
            catch (MaxRetriesExceededException)
            {
                return false;
            }
        }

        /// <summary>
        /// Asks the and verify question.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="question">The question.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <returns></returns>
        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string regexPattern)
        {
            return AskAndVerifyQuestion(minDigits, maxDigits, tries, timeout, terminators, question, InvalidAudioFile,
                                        regexPattern);
        }

        /// <summary>
        /// Asks the and verify question.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="question">The question.</param>
        /// <param name="invalidInput">The invalid input.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <returns></returns>
        public string AskAndVerifyQuestion(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                           string question, string invalidInput, string regexPattern)
        {
            CheckSessionReady();

            string result = PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators, question,
                                             invalidInput, regexPattern);

            return result;
        }

        /// <summary>
        /// Plays the and get digits.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="audioFile">The audio file.</param>
        /// <param name="badInputAudioFile">The bad input audio file.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <returns></returns>
        public string PlayAndGetDigits(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                       string audioFile, string badInputAudioFile, string regexPattern)
        {
            return PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators,
                                                            audioFile,
                                                            badInputAudioFile, regexPattern, DefaultDigitTimeout);
        }

        /// <summary>
        /// Plays the and get digits.
        /// </summary>
        /// <param name="minDigits">The min digits.</param>
        /// <param name="maxDigits">The max digits.</param>
        /// <param name="tries">The tries.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="terminators">The terminators.</param>
        /// <param name="audioFile">The audio file.</param>
        /// <param name="badInputAudioFile">The bad input audio file.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <param name="digitTimeout">The timeout for each digit.</param>
        /// <returns></returns>
        public string PlayAndGetDigits(int minDigits, int maxDigits, int tries, int timeout, string terminators,
                                       string audioFile, string badInputAudioFile, string regexPattern, int digitTimeout)
        {
            CheckSessionReady();

            var digitsReturned = Session.PlayAndGetDigits(minDigits, maxDigits, tries, timeout, terminators,
                                                            PhraseStart + audioFile,
                                                            badInputAudioFile, regexPattern, String.Empty, digitTimeout, null);
            CheckSessionReady();
            if (string.IsNullOrEmpty(digitsReturned))
                throw new MaxRetriesExceededException();

            return digitsReturned;
        }


        /// <summary>
        /// Checks if session ready.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfSessionReady()
        {
            return Session.Ready();
        }

        /// <summary>
        /// Checks the session ready.
        /// </summary>
        private void CheckSessionReady()
        {
            if (!Session.Ready()) throw new HangupException();
        }

        /// <summary>
        /// Asks the record and verify question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="recordFileName">Name of the record file.</param>
        /// <returns></returns>
        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, 60);
        }

        /// <summary>
        /// Asks the record and verify question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="recordFileName">Name of the record file.</param>
        /// <param name="timeLimit">The time limit.</param>
        /// <returns></returns>
        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName, int timeLimit)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, timeLimit, 2);
        }

        /// <summary>
        /// Asks the record and verify question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="recordFileName">Name of the record file.</param>
        /// <param name="timeLimit">The time limit.</param>
        /// <param name="silenceHits">The silence hits.</param>
        /// <returns></returns>
        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion, string recordFileName, int timeLimit, int silenceHits)
        {
            return AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, InvalidAudioFile, timeLimit, 500, silenceHits);
        }

        /// <summary>
        /// Asks the record and verify question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="isCorrectQuestion">The is correct question.</param>
        /// <param name="recordFileName">Name of the record file.</param>
        /// <param name="timeLimit">The time limit.</param>
        /// <param name="silenceThreshold">The silence threshold.</param>
        /// <param name="silenceHits">The silence hits.</param>
        /// <returns></returns>
        public string AskRecordAndVerifyQuestion(string question, Func<string, string> isCorrectQuestion,
            string recordFileName, string invalidInput, int timeLimit, int silenceThreshold, int silenceHits)
        {
            var fileNameCombined = Path.Combine(RecordingFolder, recordFileName) + ".wav";

            CheckSessionReady();
            Session.StreamFile(PhraseStart + question, 0);

            RecordToFile(fileNameCombined, timeLimit, silenceThreshold, silenceHits);


            var recordingCorrect = isCorrectQuestion(fileNameCombined);
            CheckSessionReady();
            var isRecordingFine = AskToVerifyAnswer(invalidInput, recordingCorrect);

            if (!isRecordingFine)
            {
                fileNameCombined = AskRecordAndVerifyQuestion(question, isCorrectQuestion, recordFileName, invalidInput, timeLimit, silenceThreshold,
                                           silenceHits);
            }

            return fileNameCombined;
        }

        /// <summary>
        /// Records to file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="timeLimit">The time limit.</param>
        /// <param name="silenceThreshold">The silence threshold.</param>
        /// <param name="silenceSeconds">The silence seconds.</param>
        private void RecordToFile(string fileName, int timeLimit, int silenceThreshold, int silenceSeconds)
        {
            Func<char, TimeSpan, string> receivedFunction = (c, t) => (c == '#') ? "break" : "";
            Session.DtmfReceivedFunction += receivedFunction;

            CheckSessionReady();
            Session.RecordFile(fileName, timeLimit, silenceThreshold, silenceSeconds);
            CheckSessionReady();

            Session.DtmfReceivedFunction -= receivedFunction;
        }
    }
}