using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace AlkalineThunder.ReadAloudSupport
{
    /// <summary>
    /// A wrapper for the Windows speech synthesis API that makes sure no two pieces of text are spoken at once.
    /// </summary>
    internal class TtsManager : IDisposable
    {
        // Are we speaking?
        private bool _speaking = false;

        // Queue of text to be read after the current text is finished being spoken.
        private Queue<string> _textQueue = new Queue<string>();

        // The Windows speech synthesizer we're going to use.
        private SpeechSynthesizer _synth = new SpeechSynthesizer();

        public TtsManager()
        {
            _synth.SpeakCompleted += HandleSpeakCompleted;
        }

        private void HandleSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            _speaking = false;
            ReadNextTextItem();
        }

        public void PrepareSpeech(string textToSpeak)
        {
            // Do NOT read  whitespace.
            if (string.IsNullOrWhiteSpace(textToSpeak))
                return;

            // Enqueue  the text to be read.
            _textQueue.Enqueue(textToSpeak);

            // Update the speech status.
            ReadNextTextItem();
        }

        private void ReadNextTextItem()
        {
            // If we're still speaking, there's nothing to do.
            if (_speaking) return;

            // If there's nothing left in the queue, there's nothing to do.
            if (!_textQueue.Any()) return;

            // Read the text from the queue and send it to the synth.
            var text = _textQueue.Dequeue();
            _synth.SpeakAsync(text);
        }

        public void Dispose()
        {
            _speaking = false;
            _textQueue.Clear();
            _synth.Dispose();
        }
    }
}
