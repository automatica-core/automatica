using P3.PixooSharp.Assets;

namespace P3.PixooSharp.Commands
{
    internal class SendTextCommand
    {
        public string Command { get; } = "Draw/SendHttpText";
        public int TextId { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public Direction dir { get; set; }
        public int font { get; set; } = 2;
        public int TextWidth { get; set; } = 64;
        public int speed { get; set; } = 10;
        public string TextString { get; set; }
        public string color { get; set; }

        public SendTextCommand(int textId, int xPos, int yPos, Direction direction, string Text, Rgb colour, int fontId = 2, int textWidth = 64, int textSpeed = 10)
        {
            TextId = textId;
            x = xPos;
            y = yPos;
            dir = direction;
            font = fontId;
            TextWidth = textWidth;
            speed = textSpeed;
            TextString = Text;
            // need to convert to hex colour
            color = string.Format("#{0:X2}{1:X2}{2:X2}", colour.Red, colour.Green, colour.Blue);
        }
    }
}
