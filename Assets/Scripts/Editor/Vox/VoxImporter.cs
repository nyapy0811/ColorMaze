using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColorMaze
{
    /// <summary>
    /// MagicaVoxel <c>.vox</c>를 프리팹 겉모습으로 가져온다(약속은 Docs/v2 복셀 모델링 규칙 2~5절). 오브젝트 하나가 자식 하나이고,
    /// 칠하는 색(값 색 248, 연결색 249)의 칸은 자식 아래 별도 렌더러(흰색)로 떼어낸다. 기물 컴포넌트가 그 렌더러를 통째로 칠한다.
    /// 나머지 칸은 파일의 팔레트를 256 × 1 텍스처로 만들어 면마다 자기 색 칸을 가리키게 한다(URP Lit은 버텍스 색을 읽지 않음).
    /// </summary>
    [ScriptedImporter(1, "vox")]
    public class VoxImporter : ScriptedImporter
    {
        public enum PivotMode { BottomBackEdge, BottomCenter, TopCenter }

        /// <summary>한 칸의 길이(유닛). 1유닛 = 20칸.</summary>
        public const float VoxelSize = 0.05f;

        public const byte ValueColor = 248;
        public const byte LinkColor = 249;
        const byte FirstSpareColor = 250;

        static readonly (byte color, string name)[] Paints = { (ValueColor, "ValueColor"), (LinkColor, "LinkColor") };

        [Tooltip("기준점. 모든 오브젝트의 모델 크기 상자를 합친 상자에서 고른다(규칙 5절): 바닥면 뒤쪽 가장자리의 가운데(기물 기준, 기본), " +
                 "바닥면 가운데(벽 모듈, 기둥, 끼운 문), 윗면 가운데(바닥·천장 판, 천장에 붙는 것). 자식은 자기 상자에서 같은 쪽을 기준점으로 쓴다.")]
        public PivotMode pivot = PivotMode.BottomBackEdge;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var file = VoxFile.Read(File.ReadAllBytes(ctx.assetPath));
            Warn(ctx, file);

            var palette = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                name = "Palette",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            palette.SetPixels32(file.palette);
            palette.Apply();
            ctx.AddObjectToAsset("palette", palette);
            var voxelMaterial = NewMaterial("Voxel", palette);
            var paintMaterial = NewMaterial("Paint", null);
            ctx.AddObjectToAsset("material:Voxel", voxelMaterial);
            ctx.AddObjectToAsset("material:Paint", paintMaterial);

            var root = new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
            var (min, max) = file.Bounds();
            var rootPivot = Pivot(min, max);
            var names = new HashSet<string>();
            for (int i = 0; i < file.objects.Count; i++)
            {
                var obj = file.objects[i];
                // 다시 가져와도 프리팹의 참조가 이어지게 이름(= 하위 애셋 id)이 겹치지 않게 한다.
                string name = obj.name ?? $"Object {i + 1}";
                for (int n = 2; !names.Add(name); n++) name = $"{obj.name ?? $"Object {i + 1}"} ({n})";

                var childPivot = Pivot(obj.Min, obj.Max);
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = (childPivot - rootPivot) * VoxelSize;

                var grid = VoxMesher.Grid(obj.model);
                var offset = ToUnity(obj.Min) - childPivot;
                var plain = VoxMesher.Quads(grid, color => color != ValueColor && color != LinkColor);
                if (plain.Count > 0) Attach(ctx, child, name, BuildMesh(name, plain, offset), voxelMaterial);
                foreach (var (color, partName) in Paints)
                {
                    var quads = VoxMesher.Quads(grid, c => c == color);
                    if (quads.Count == 0) continue;
                    var part = new GameObject(partName);
                    part.transform.SetParent(child.transform, false);
                    Attach(ctx, part, $"{name}/{partName}", BuildMesh(partName, quads, offset), paintMaterial);
                }
            }
            ctx.AddObjectToAsset("root", root);
            ctx.SetMainObject(root);
        }

        /// <summary>상자(MagicaVoxel 칸 좌표)의 기준점을 Unity 축 칸 좌표로 준다. 뒤쪽은 MagicaVoxel −Y(Unity −Z, 앞면의 반대쪽)다.</summary>
        Vector3 Pivot(Vector3Int min, Vector3Int max)
        {
            float x = (min.x + max.x) / 2f;
            float depth = (min.y + max.y) / 2f;
            return pivot switch
            {
                PivotMode.BottomBackEdge => new Vector3(x, min.z, min.y),
                PivotMode.TopCenter => new Vector3(x, max.z, depth),
                _ => new Vector3(x, min.z, depth),
            };
        }

        static Vector3 ToUnity(Vector3Int position) => new(position.x, position.z, position.y);

        static void Attach(AssetImportContext ctx, GameObject target, string id, Mesh mesh, Material material)
        {
            ctx.AddObjectToAsset("mesh:" + id, mesh);
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            target.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>사각형을 메시로 만든다. offset(칸)만큼 옮겨 칸 크기를 곱하고, UV는 팔레트 텍스처의 자기 색 칸 가운데를 가리킨다.</summary>
        static Mesh BuildMesh(string name, List<VoxMesher.Quad> quads, Vector3 offset)
        {
            var vertices = new List<Vector3>(quads.Count * 4);
            var normals = new List<Vector3>(quads.Count * 4);
            var uvs = new List<Vector2>(quads.Count * 4);
            var triangles = new List<int>(quads.Count * 6);
            foreach (var quad in quads)
            {
                int start = vertices.Count;
                Vector3 origin = quad.origin + offset;
                vertices.Add(origin * VoxelSize);
                vertices.Add((origin + quad.du) * VoxelSize);
                vertices.Add((origin + quad.du + quad.dv) * VoxelSize);
                vertices.Add((origin + quad.dv) * VoxelSize);
                var uv = new Vector2((quad.color + 0.5f) / 256f, 0.5f);
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(quad.normal);
                    uvs.Add(uv);
                }
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }

        /// <summary>
        /// 파일 안 모든 렌더러가 같이 쓰는 재질. 칠하기와 상호작용 강조는 기물 컴포넌트가 MaterialPropertyBlock으로 렌더러마다 바꾸므로
        /// 발광을 켜 두고(검정) 나머지는 임시 회색 재질(Temp)과 맞춘다.
        /// </summary>
        static Material NewMaterial(string name, Texture2D baseMap)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetTexture("_BaseMap", baseMap);
            material.SetFloat("_Smoothness", 0.2f);
            material.SetColor("_EmissionColor", Color.black);
            // 발광 색이 검정이어도 키워드가 남게 한다(EmissiveIsBlack이면 URP가 _EMISSION을 끈다).
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword("_EMISSION");
            return material;
        }

        /// <summary>규칙 2절: 예약 칸이 아닌 칸은 회색(R = G = B)만 쓰고 예비 칸(250~255)은 쓰지 않는다. 규칙 5절: 오브젝트 회전은 쓰지 않는다.</summary>
        static void Warn(AssetImportContext ctx, VoxFile file)
        {
            if (file.hasRotation) ctx.LogImportWarning("[VoxImporter] 오브젝트 회전(_r)이 있지만 읽지 않는다(회전 없이 놓임).");
            var used = file.models.SelectMany(model => model.voxels).Select(voxel => voxel.color).Distinct().OrderBy(color => color).ToList();
            var spare = used.Where(color => color >= FirstSpareColor).ToList();
            var colored = used.Where(color => color < ValueColor && !IsGray(file.palette[color])).ToList();
            if (spare.Count > 0) ctx.LogImportWarning($"[VoxImporter] 예비 칸(250~255)을 썼다: 색 번호 {string.Join(", ", spare)}");
            if (colored.Count > 0) ctx.LogImportWarning($"[VoxImporter] 회색이 아닌 일반 칸을 썼다: 색 번호 {string.Join(", ", colored)}");
        }

        static bool IsGray(Color32 color) => color.r == color.g && color.g == color.b;
    }
}
