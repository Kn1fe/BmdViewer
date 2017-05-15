using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace BmdViewer
{
    public class Vertex
    {
        public Point3D position { get; set; }
        public int unk1 { get; set; }
        public Point uvcoord;

        public Vertex(BinaryReader br)
        {
            position = new Point3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            unk1 = br.ReadInt32();
            uvcoord = new Point(br.ReadSingle(), br.ReadSingle());
        }
    }

    public class PWMesh
    {
        public short version { get; set; }
        public short unk1 { get; set; }
        public byte unk2 { get; set; }
        public string name { get; set; }
        public string[] texture { get; set; }
        public int vertexCount { get; set; }
        public int facesCount { get; set; }
        public byte unk3 { get; set; }
        public List<Vertex> vertices = new List<Vertex>();
        public List<short[]> faces = new List<short[]>();
        public List<Vector3D> normals = new List<Vector3D>();
        public List<byte[]> unk = new List<byte[]>();
        public Location loc { get; set; }
        //Material
        public byte[] header { get; set; }
        public float[] values = new float[16];
        public float scale { get; set; }
        public byte isClothing { get; set; }
        public byte unk4 { get; set; }
        public List<float[]> su = new List<float[]>();

        public PWMesh(BinaryReader br, short unk5)
        {
            version = br.ReadInt16();
            unk1 = br.ReadInt16();
            unk2 = unk1 == 1 ? br.ReadByte() : (byte)0;
            name = Utils.toGBKString(br.ReadBytes(64));
            texture = new string[4];
            for (int i = 0; i < 4; ++i)
                texture[i] = Utils.toGBKString(br.ReadBytes(64));
            vertexCount = br.ReadInt32();
            facesCount = br.ReadInt32();
            unk3 = version == 6 ? br.ReadByte() : (byte)0;
            for (int i = 0; i < vertexCount; ++i)
                vertices.Add(new Vertex(br));
            for (int i = 0; i < facesCount; ++i)
                faces.Add(new short[] { br.ReadInt16(), br.ReadInt16(), br.ReadInt16() });
            for (int i = 0; i < vertexCount; ++i)
                normals.Add(new Vector3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
            for (int i = 0; i < vertexCount; ++i)
                unk.Add(br.ReadBytes(8));
            loc = new Location(br);
            //Material
            header = br.ReadBytes(11);
            for (int i = 0; i < 16; ++i)
                values[i] = br.ReadSingle();
            scale = br.ReadSingle();
            isClothing = br.ReadByte();
            unk1 = scale > 0 ? br.ReadByte() : (byte)0;
            if (unk5 > 2)
            {
                for (int i = 0; i < vertexCount; ++i)
                    su.Add(new float[] { br.ReadSingle(), br.ReadSingle() });
            }
        }
    }

    public class Location
    {
        public float scaleX { get; set; }
        public float scaleY { get; set; }
        public float scaleZ { get; set; }
        public float directionX { get; set; }
        public float directionY { get; set; }
        public float directionZ { get; set; }
        public float upX { get; set; }
        public float upY { get; set; }
        public float upZ { get; set; }
        public float positionX { get; set; }
        public float positionY { get; set; }
        public float positionZ { get; set; }

        public Location(BinaryReader br)
        {
            scaleX = br.ReadSingle();
            scaleY = br.ReadSingle();
            scaleZ = br.ReadSingle();
            directionX = br.ReadSingle();
            directionY = br.ReadSingle();
            directionZ = br.ReadSingle();
            upX = br.ReadSingle();
            upY = br.ReadSingle();
            upZ = br.ReadSingle();
            positionX = br.ReadSingle();
            positionY = br.ReadSingle();
            positionZ = br.ReadSingle();
        }
    }

    public class BMDFile
    {
        public byte[] header { get; set; }
        public int version { get; set; }
        public byte collideOnly { get; set; }
        public short unk1 { get; set; }
        public short unk2 { get; set; }
        public Location loc { get; set; }
        int numModels;
        public List<PWMesh> meshs = new List<PWMesh>();


        public void Read(string path)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(path));
            header = br.ReadBytes(4);
            version = br.ReadInt32();
            collideOnly = version == -2147483647 ? br.ReadByte() : (byte)0;
            unk1 = br.ReadInt16();
            unk2 = br.ReadInt16();
            loc = new Location(br);
            numModels = br.ReadInt32();
            for (int i = 0; i < numModels; ++i)
                meshs.Add(new PWMesh(br, unk1));
        }

        public Model3DGroup GetModel()
        {
            Model3DGroup model3DGroup = new Model3DGroup();
            meshs.ForEach(x =>
            {
                List<Point3D> list1 = new List<Point3D>();
                List<Vector3D> list2 = new List<Vector3D>();
                List<Point> list3 = new List<Point>();
                for (int i = 0; i < x.vertices.Count; ++i)
                {
                    list1.Add(x.vertices[i].position);
                    list2.Add(x.normals[i]);
                    list3.Add(x.vertices[i].uvcoord);
                }
                List<int> list4 = new List<int>();
                x.faces.ForEach(y =>
                {
                    list4.Add(y[0]);
                    list4.Add(y[1]);
                    list4.Add(y[2]);
                });
                MeshGeometry3D geometry = new MeshGeometry3D
                {
                    Normals = new Vector3DCollection(list2),
                    Positions = new Point3DCollection(list1),
                    TextureCoordinates = new PointCollection(list3),
                    TriangleIndices = new Int32Collection(list4)
                };
                Material material = Materials.Gray;
                byte[] buffer = MainWindow.Buildings.ReadFile(x.texture[0].ToLower());
                if (buffer.Length > 0)
                {
                    material = MaterialHelper.CreateImageMaterial(ToImage(buffer), 1.0);
                }
                model3DGroup.Children.Add(new GeometryModel3D
                {
                    Geometry = geometry,
                    Material = material,
                    BackMaterial = material
                });
            });
            return model3DGroup;
        }

        public BitmapImage ToImage(byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }
    }
}
