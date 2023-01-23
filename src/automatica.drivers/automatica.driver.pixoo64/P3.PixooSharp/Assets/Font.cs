namespace P3.PixooSharp.Assets
{
    public class Font
    {
        public FontCharacter[] FontPico8 { get; set; }

        public Font()
        {
            FontPico8 = new FontCharacter[]
            {
                new FontCharacter()
                {
                    Character = '1',
                    Data = new byte[] {1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 1}
                },
                new FontCharacter()
                {
                    Character = '2',
                    Data = new byte[] {1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1}
                },
                new FontCharacter()
                {
                    Character = '3',
                    Data = new byte[] {1, 1, 1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1}
                },
                new FontCharacter()
                {
                    Character= '4',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '5',
                    Data= new byte[] { 1, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '6',
                    Data= new byte[] { 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '7',
                    Data= new byte[] { 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '8',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '9',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '0',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'a',
                    Data= new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'b',
                    Data= new byte[] { 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'c',
                    Data= new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character = 'd',
                    Data = new byte[] { 0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character = 'e',
                    Data = new byte[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'f',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'g',
                    Data= new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'h',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'i',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'j',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'k',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character = 'l',
                    Data = new byte[] { 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'm',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'n',
                    Data= new byte[] { 0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character = 'o',
                    Data = new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'p',
                    Data= new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'q',
                    Data= new byte[] { 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character = 'r',
                    Data = new byte[] { 0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'r',
                    Data= new byte[] { 0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 's',
                    Data= new byte[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 't',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'u',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'v',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'w',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'x',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'y',
                    Data= new byte[] { 0, 0, 0, 1, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'z',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'A',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'B',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'C',
                    Data= new byte[] { 0, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'D',
                    Data= new byte[] { 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'E',
                    Data= new byte[] { 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'F',
                    Data= new byte[] { 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'G',
                    Data= new byte[] { 0, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character = 'H',
                    Data = new byte[] { 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'I',
                    Data= new byte[] { 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'J',
                    Data= new byte[] { 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'K',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'L',
                    Data= new byte[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'M',
                    Data= new byte[] { 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'N',
                    Data= new byte[] { 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'O',
                    Data= new byte[] { 0, 1, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'P',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'Q',
                    Data= new byte[] { 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'R',
                    Data= new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'S',
                    Data= new byte[] { 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'T',
                    Data= new byte[] { 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'U',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'V',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character = 'W',
                    Data = new byte[] { 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'X',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= 'Y',
                    Data= new byte[] { 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= 'Z',
                    Data= new byte[] { 1, 1, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '!',
                    Data= new byte[] { 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '\'',
                    Data= new byte[] { 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '(',
                    Data= new byte[] { 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= ')',
                    Data= new byte[] { 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '+',
                    Data= new byte[] { 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= ',',
                    Data= new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '-',
                    Data= new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '<',
                    Data= new byte[] { 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '=',
                    Data= new byte[] { 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '>',
                    Data= new byte[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '?',
                    Data= new byte[] { 1, 1, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '[',
                    Data= new byte[] { 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= ']',
                    Data= new byte[] { 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '^',
                    Data= new byte[] { 0, 1, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '_',
                    Data= new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= ':',
                    Data= new byte[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0 }
                },
                new FontCharacter()
                {
                    Character= ';',
                    Data= new byte[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '.',
                    Data= new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '/',
                    Data= new byte[] { 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '{',
                    Data= new byte[] { 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '|',
                    Data= new byte[] { 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '}',
                    Data= new byte[] { 1, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '~',
                    Data= new byte[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '$',
                    Data= new byte[] { 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '@',
                    Data= new byte[] { 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 0, 0, 1, 1 }
                },
                new FontCharacter()
                {
                    Character= '%',
                    Data= new byte[] { 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1 }
                },
                new FontCharacter()
                {
                    Character= '°',
                    Data= new byte[] { 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
                }
            };
        }

        public byte[] GetCharacterMatrix(char character)
        {
            var res = FontPico8.FirstOrDefault(c => c.Character == character);
            if (res == null)
            {
                return new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            }
            else
            {
                return res.Data;
            }

        }
    }

    public class FontCharacter
    {
        public char Character { get; set; }
        public byte[] Data { get; set; }
    }
}
