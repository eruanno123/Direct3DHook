

namespace TestScreenshot
{
    using System;

    class HSaveItem
    {
        public string SaveFilePath { get; set; }
        public string ScreenCaptureFilePath { get; set; }
        public DateTime CaptureTime { get; set; }
        public string Checksum { get; set; }
        public string LocationName { get; set; }


        public override string ToString ()
        {
            return $"[{CaptureTime} {LocationName} ({Checksum})]";
        }
    }
}
