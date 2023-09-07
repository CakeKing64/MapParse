using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MapParse.Types;
using MapParse.Util;

namespace MapParse
{
    public static class MapParser
    {
        private enum FaceParseState
        {
            PLANE,
            TEXTURE,
            TEXAXIS,
            ROTATION,
            SCALEX,
            SCALEY
        }

        private enum Vec3ParseState
        {
            X,
            Y,
            Z
        }

        private enum PlaneParseState
        {
            N_X, // x, y, and z values of the normal
            N_Y,
            N_Z,
            DISTANCE
        }

        private static StringBuilder sb = new StringBuilder();

        private static int i = 0;

        public static MapFile ParseMap(string pathToMapFile)
        {
            string contents = File.ReadAllText(pathToMapFile);
            return ParseMapString(contents);
        }

        public static MapFile ParseMapString(string content)
        {
            MapFile map = new MapFile();

            int index = 0;
            char token;
            bool done = false;
            while (!done)
            {
                token = content[index];
                if (token == Constants.LEFT_BRACE)
                {
                    Entity entity = parseEntity(content, ref index);
                    map.Entities.Add(entity);
                }

                if (index == content.Length - 1)
                {
                    done = true;
                    break;
                }

                index++;
            }

            return map;
        }

        private static Entity parseEntity(string content, ref int index)
        {
            Entity entity = new Entity();

            index++;

            bool done = false;
            char token;
            while (!done)
            {
                token = content[index];

                if (PeekChar(content, index) == Constants.QUOTATION_MARK)
                {
                    Property p = parseProperty(content, ref index);
                    entity.AddProperty(p);
                    index++;
                    continue;
                }

                if (PeekChar(content, index) == Constants.LEFT_BRACE)
                {
                    Brush b = parseBrush(content, ref index);
                    entity.Brushes.Add(b);
                    index++;
                    continue;
                }

                if (PeekChar(content, index) == Constants.RIGHT_BRACE)
                {
                    done = true;
                    break;
                }

                index++;
            }

            return entity;
        }

        private static Property parseProperty(string content, ref int index)
        {
            Property p = new Property();
            p.Key = GetString(content, ref index);
            p.Value = GetString(content, ref index);

            return p;
        }

        private static Brush parseBrush(string content, ref int index)
        {
            Brush b = new Brush();

            while (PeekChar(content, index) != Constants.RIGHT_BRACE)
            {
                SkipChar(content, ref index); //  Skip the {

                Face f = parseFace(content, ref index);
                b.Faces.Add(f);
            }
            SkipChar(content, ref index);

            b.GeneratePolys();
            for (int i = 0; i < b.NumberOfFaces; i++)
            {
                for (int j = 0; j < b.Faces[i].Polys.Length; j++)
                {
                    Poly poly = b.Faces[i].Polys[j];
                    poly.P = b.Faces[i].P;
                    poly.SortVerticesCW();
                    b.Faces[i].Polys[j] = poly;
                }
            }

            //b.CalculateAABB();
            return b;
        }

        private static void StringDebug(string content, int index)
        {
            for (int i = index; i < content.Length; i++)
            {
                Console.Write(content[i]);
            }

        }

        private static Face parseFace(string content, ref int index)
        {
            Face f = new Face();
            Vec3[] plane = new Vec3[3];
            string texture = "";
            Plane[] texAxis = new Plane[2];
            double rotation = 0F;
            double[] scale = new double[2];

            for (int i = 0; i < 3; i++)
            {
                plane[i] = parseVec3(content, ref index);
            }

            texture = GetString(content, ref index);
            
            for(int i = 0; i < 2;i++)
            {
                texAxis[i] = parsePlane(content, ref index);
            }

            rotation = double.Parse(GetString(content, ref index));
            scale[0] = double.Parse(GetString(content, ref index));
            scale[1] = double.Parse(GetString(content, ref index));

            f.P = new Plane(plane[0], plane[1], plane[2]);
            f.Texture = texture;
            f.TexAxis = texAxis;
            f.Rotation = rotation;
            f.TexScale = scale;
            return f;
        }

        private static Plane parsePlane(string content, ref int index)
        {
            Plane p = new Plane();
            SkipChar(content, ref index);
            p.Normal.X = double.Parse(GetString(content, ref index));
            p.Normal.Z = double.Parse(GetString(content, ref index));
            p.Normal.Y = double.Parse(GetString(content, ref index));
            p.Distance = double.Parse(GetString(content, ref index));
            SkipChar(content, ref index);

            return p;
        }

        private static Vec3 parseVec3(string content, ref int index)
        {
            Vec3 vec3 = new Vec3();

            SkipChar(content, ref index);
            vec3.X = double.Parse(GetString(content, ref index));
            vec3.Z = double.Parse(GetString(content, ref index));
            vec3.Y = double.Parse(GetString(content, ref index));
            SkipChar(content, ref index);

            return vec3;
        }


        private static char LookAhead(string content, int index) { return content[index + 1]; }


        private static readonly Dictionary<char, string> EscapeCodes
    = new Dictionary<char, string>
    {
                { '\'', "'" },
                { '\"', "\"" },
                { '\\', "\\" },
                { '\n', "\n" },
                { '\r', "\r" },
                { '\t', "\t" },
                { '\0', "\0" },
    };
        private static char PeekChar(string content, int index)
        {
            int index2 = index;
            char c = content[index2];
            while (char.IsWhiteSpace(c))
            {
                c = content[index2++];
            }

            //Console.WriteLine("PeekChar() " + c);
            return c;
        }
        private static void SkipChar(string content, ref int index)
        {
            char c = content[index];
            while (char.IsWhiteSpace(c))
            {
                c = content[index++];
            }

            index++;

            //Console.WriteLine("SkipChar() " + c);
        }
        private static string GetString(string content, ref int index)
        {
            string str = "";
            char c = content[index]; 
            int qt = 0;

            while (char.IsWhiteSpace(c))
            {
                c = content[index];
                if (char.IsWhiteSpace(c))
                    index++;
            }

            if (c != '\"' && c != '\'')
            {
                qt = 3;
            }

            while (qt != 2)
            {
                c = content[index++];

                if (char.IsWhiteSpace(c) && qt == 3)
                {
                    qt = 2;
                    continue;
                }
                // std::cout << c;
                switch (c)
                {
                    case '"':
                        qt++;
                        continue;
                    case '\\':
                        {
                            var escape = content[index++];
                            string escapeC;

                            if (EscapeCodes.TryGetValue(escape, out escapeC))
                            {
                                str += escapeC;
                            }
                            else
                                str += escape;
                            continue;
                        }
                    default:
                        str += c;
                        break;
                }


            }
            //Console.WriteLine("GetString() \"" + str + "\"");
            return str;
        }
    }
}
