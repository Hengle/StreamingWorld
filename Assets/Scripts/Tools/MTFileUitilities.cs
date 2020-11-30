namespace MightyTerrainMesh
{
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using System.IO;
    using UnityEngine.Networking;
    using UnityEditor;

    public static class MTFileUtils
    {
        private static void WriteVector3(FileStream stream, Vector3 v)
        {
            byte[] sBuff = BitConverter.GetBytes(v.x);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.y);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.z);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        private static Vector3 ReadVector3(FileStream stream)
        {
            Vector3 v = Vector3.zero;
            byte[] sBuff = new byte[sizeof(float)];
            stream.Read(sBuff, 0, sizeof(float));
            v.x = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.y = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.z = BitConverter.ToSingle(sBuff, 0);
            return v;
        }
        private static void WriteVector2(FileStream stream, Vector2 v)
        {
            byte[] sBuff = BitConverter.GetBytes(v.x);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.y);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        private static Vector2 ReadVector2(FileStream stream)
        {
            Vector2 v = Vector2.zero;
            byte[] sBuff = new byte[sizeof(float)];
            stream.Read(sBuff, 0, sizeof(float));
            v.x = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.y = BitConverter.ToSingle(sBuff, 0);
            return v;
        }

        public static void SaveQuadTreeHeader(string dataName, MTQuadTreeHeader header, int matCount)
        {
            string path = string.Format("{0}/{1}/{2}.bytes", Application.streamingAssetsPath, dataName, dataName);

            if (File.Exists(path))
                File.Delete(path);
            FileStream stream = File.Open(path, FileMode.Create);
            byte[] uBuff = BitConverter.GetBytes(header.QuadTreeDepth);
            stream.Write(uBuff, 0, sizeof(int));
            WriteVector3(stream, header.BoundMin);
            WriteVector3(stream, header.BoundMax);
            //lod count
            uBuff = BitConverter.GetBytes(header.LOD);
            stream.Write(uBuff, 0, sizeof(int));
            //material count
            uBuff = BitConverter.GetBytes(matCount);
            stream.Write(uBuff, 0, sizeof(int));
            //blocks 
            uBuff = BitConverter.GetBytes(header.Meshes.Count);
            stream.Write(uBuff, 0, sizeof(int));
            foreach (var v in header.Meshes.Values)
            {
                uBuff = BitConverter.GetBytes(v.MeshID);
                stream.Write(uBuff, 0, sizeof(int));
                WriteVector3(stream, v.Center);
            }
            stream.Close();
        }

        public static MTQuadTreeHeader LoadQuadTreeHeader(string dataName)
        {
            MTQuadTreeHeader header = new MTQuadTreeHeader(dataName);
            string path = string.Format("{0}/{1}/{2}.bytes", Application.streamingAssetsPath, dataName, dataName);
            FileStream stream = File.Open(path, FileMode.Open);
            int byteLen = sizeof(int);
            byte[] nBuff = new byte[byteLen];
            stream.Read(nBuff, 0, byteLen);
            header.QuadTreeDepth = BitConverter.ToInt32(nBuff, 0);
            header.BoundMin = ReadVector3(stream);
            header.BoundMax = ReadVector3(stream);
            //lod count
            stream.Read(nBuff, 0, byteLen);
            header.LOD = BitConverter.ToInt32(nBuff, 0);
            //material count
            stream.Read(nBuff, 0, byteLen);
            int matCount = BitConverter.ToInt32(nBuff, 0);
            header.RuntimeMats = new Material[matCount];
            //blocks 
            stream.Read(nBuff, 0, byteLen);
            int len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
            {
                stream.Read(nBuff, 0, byteLen);
                int meshid = BitConverter.ToInt32(nBuff, 0);
                Vector3 c = ReadVector3(stream);
                header.Meshes.Add(meshid, new MTMeshHeader(meshid, c));
            }
            stream.Close();

            //init material
            for (int i = 0; i < header.RuntimeMats.Length; ++i)
            {
                string mathPath = string.Format("{0}_{1}", header.DataName, i);
                header.RuntimeMats[i] = Resources.Load<Material>(mathPath);
            }
            return header;

        }

        public static void SaveMesh(string dataName, MTMeshData data)
        {
            string path = string.Format("{0}/{1}/{2}_{3}.bytes", Application.streamingAssetsPath, dataName, dataName, data.meshId);
            if (File.Exists(path))
                File.Delete(path);
            FileStream stream = File.Open(path, FileMode.Create);
            //lods
            byte[] uBuff = BitConverter.GetBytes(data.lods.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var lod in data.lods)
            {
                //vertices
                uBuff = BitConverter.GetBytes(lod.vertices.Length);
                stream.Write(uBuff, 0, uBuff.Length);
                foreach (var v in lod.vertices)
                    WriteVector3(stream, v);
                //normals
                uBuff = BitConverter.GetBytes(lod.normals.Length);
                stream.Write(uBuff, 0, uBuff.Length);
                foreach (var n in lod.normals)
                    WriteVector3(stream, n);
                //uvs
                uBuff = BitConverter.GetBytes(lod.uvs.Length);
                stream.Write(uBuff, 0, uBuff.Length);
                foreach (var uv in lod.uvs)
                    WriteVector2(stream, uv);
                //faces
                uBuff = BitConverter.GetBytes(lod.faces.Length);
                stream.Write(uBuff, 0, uBuff.Length);
                foreach (var face in lod.faces)
                {
                    uBuff = BitConverter.GetBytes(face);
                    stream.Write(uBuff, 0, uBuff.Length);
                }
            }
            stream.Close();
        }

        public static void SaveMeshAsset(string dataName, MTMeshData data)
        {
#if UNITY_EDITOR

            for (int i = 0; i < data.lods.Length; i++)
            {
                string meshName = string.Format("{0}_{1}_LOD {2}", dataName, data.meshId, i);
                string assetName = string.Format("Assets/Resources/{0}/{1}.asset", dataName, meshName);
                Mesh mesh = new Mesh();
                mesh.vertices = data.lods[i].vertices;
                mesh.normals = data.lods[i].normals;
                mesh.uv = data.lods[i].uvs;
                mesh.triangles = data.lods[i].faces;
                AssetDatabase.CreateAsset(mesh, assetName);
                mesh.name = meshName;
            }
#endif            
        }

        private static List<Vector3> _vec3Cache = new List<Vector3>();
        private static List<Vector2> _vec2Cache = new List<Vector2>();
        private static List<int> _intCache = new List<int>();
        public static void LoadMesh(Mesh[] meshes, string dataName, int meshId)
        {
            string path = string.Format("{0}/{1}/{2}_{3}.bytes", Application.streamingAssetsPath, dataName, dataName, meshId);
            FileStream stream = File.Open(path, FileMode.Open);
            _vec2Cache.Clear();
            _intCache.Clear();
            //lods
            int byteLen = sizeof(int);
            byte[] nBuff = new byte[byteLen];
            stream.Read(nBuff, 0, byteLen);
            int lods = BitConverter.ToInt32(nBuff, 0);
            if (meshes.Length != lods)
            {
                MTLog.LogError("meshes length does not match lods");
                return;
            }
            for (int l = 0; l < lods; ++l)
            {
                meshes[l] = new Mesh();
                //vertices
                _vec3Cache.Clear();
                nBuff = new byte[byteLen];
                stream.Read(nBuff, 0, byteLen);
                int len = BitConverter.ToInt32(nBuff, 0);
                for (int i = 0; i < len; ++i)
                    _vec3Cache.Add(ReadVector3(stream));
                meshes[l].vertices = _vec3Cache.ToArray();
                //normals
                _vec3Cache.Clear();
                stream.Read(nBuff, 0, byteLen);
                len = BitConverter.ToInt32(nBuff, 0);
                for (int i = 0; i < len; ++i)
                    _vec3Cache.Add(ReadVector3(stream));
                meshes[l].normals = _vec3Cache.ToArray();
                //uvs
                _vec2Cache.Clear();
                stream.Read(nBuff, 0, byteLen);
                len = BitConverter.ToInt32(nBuff, 0);
                for (int i = 0; i < len; ++i)
                    _vec2Cache.Add(ReadVector2(stream));
                meshes[l].uv = _vec2Cache.ToArray();
                //faces
                _intCache.Clear();
                stream.Read(nBuff, 0, byteLen);
                len = BitConverter.ToInt32(nBuff, 0);
                byte[] fBuff = new byte[byteLen];
                for (int i = 0; i < len; ++i)
                {
                    stream.Read(fBuff, 0, byteLen);
                    _intCache.Add(BitConverter.ToInt32(fBuff, 0));
                }
                meshes[l].triangles = _intCache.ToArray();
            }
            stream.Close();
        }

        public static void LoadMeshAsset(Mesh[] meshes, string dataName, int meshId, int LODLevel)
        {
            if (meshes.Length != LODLevel)
            {
                MTLog.LogError("meshes length does not match lods");
                return;
            }

            for (int i = 0; i < LODLevel; i++)
            {
                string meshName = string.Format("{0}_{1}_LOD {2}", dataName, meshId, i);
                string assetName = string.Format("{0}/{1}", dataName, meshName);
                Debug.Log(assetName);
                meshes[i] = Resources.Load<Mesh>(assetName);
            }
        }
    }
}