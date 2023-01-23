namespace P3.PixooSharp.Commands
{
    internal class SendBufferCommand
    {
        public string Command { get; } = "Draw/SendHttpGif";
        public int PicNum { get; set; } = 1;
        public int PicWidth { get; set; }
        public int PicOffset { get; set; } = 0;
        public int PicID { get; set; }
        public int PicSpeed { get; set; } = 1000;
        public string PicData { get; set; }

        public SendBufferCommand(int picWidth, int frameId, string picData)
        {
            PicWidth = picWidth;
            PicID = frameId;
            PicData = picData;
        }

    }
}
