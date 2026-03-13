using UnityEngine;

namespace TacticsGrid
{
    public class Tile : MonoBehaviour
    {
        public Vector2Int Coord { get; private set; }
        public bool Walkable = true;
        public int TraversalBlockCount { get; private set; } = 0; // 경로에서 통과 불가
        public int OccupancyBlockCount { get; private set; } = 0; // 도착(같은 칸 공존) 불가

        public bool BlocksTraversal => TraversalBlockCount > 0;
        public bool BlocksOccupancy => OccupancyBlockCount > 0;
        public bool Occupied => OccupancyBlockCount > 0;
        public bool BlocksMovement => TraversalBlockCount > 0;

        public void AddTraversalBlock(int delta) =>
            TraversalBlockCount = Mathf.Max(0, TraversalBlockCount + delta);

        public void AddOccupancyBlock(int delta) =>
            OccupancyBlockCount = Mathf.Max(0, OccupancyBlockCount + delta);

        public void AddOccupy(int delta) =>
            OccupancyBlockCount = Mathf.Max(0, OccupancyBlockCount + delta);

        public void AddBlock(int delta) =>
            TraversalBlockCount = Mathf.Max(0, TraversalBlockCount + delta);



        [Header("Overlay (auto)")]
        [SerializeField] private MeshRenderer overlayRenderer;
        [SerializeField] private Material overlayMaterial;

        [Tooltip("타일 로컬 기준 오버레이 높이(Y). (큐브면 보통 0.52~0.55 권장)")]
        [SerializeField] private float overlayHeight = 0.52f;

        [SerializeField] private Vector2 overlayScale = new Vector2(0.95f, 0.95f);

        [Tooltip("인스펙터에서 기본 오버레이 색을 설정 (ShowOverlay 호출 시 덮어씀)")]
        [SerializeField] private Color defaultOverlayColor = new Color(0.25f, 0.55f, 1f, 0.35f);

        [Header("Overlay Sorting")]
        [Tooltip("오버레이 Sorting Layer (MeshRenderer도 지원)")]
        [SerializeField] private string overlaySortingLayerName = "Tile_Overlay";

        [Tooltip("오버레이 Order in Layer (값이 작을수록 뒤로 감). Units보다 낮게!")]
        [SerializeField] private int overlayOrderInLayer = -100;

        [Header("Overlay Render Queue (Optional)")]
        [Tooltip("0이면 건드리지 않음. 3000=Transparent. 타일별 material 인스턴스에만 적용됨.")]
        [SerializeField] private int overlayRenderQueue = 0;

        [Header("Base Renderer (optional)")]
        [SerializeField] private Renderer rend;

        private MaterialPropertyBlock mpb;

        // ✅ 타일별로만 쓰는 오버레이 material 인스턴스 (sharedMaterial 오염 방지)
        private Material overlayMaterialInstance;

        // URP / Built-in 호환
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (rend == null) rend = GetComponentInChildren<Renderer>();
            mpb ??= new MaterialPropertyBlock();

            EnsureOverlay();
            HideOverlay();
        }

        private void EnsureOverlay()
        {
            // 1) overlayRenderer가 비어있으면 자식에서 찾기
            if (overlayRenderer == null)
            {
                var existing = transform.Find("Overlay");
                if (existing != null)
                    overlayRenderer = existing.GetComponent<MeshRenderer>();
            }

            // 2) 없으면 생성
            if (overlayRenderer == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Overlay";
                go.transform.SetParent(transform, false);

                // 콜라이더 제거 (CreatePrimitive로 붙는 기본 콜라이더)
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                overlayRenderer = go.GetComponent<MeshRenderer>();
            }

            if (overlayRenderer == null) return;

            // ✅ 생성 여부와 상관없이 항상 최신 값 적용
            var tr = overlayRenderer.transform;

            // Quad → XZ로 눕힘 (한쪽면 컬링 이슈 있으면 180 유지)
            tr.localRotation = Quaternion.Euler(90f, 0f, 180f);
            tr.localPosition = new Vector3(0f, overlayHeight, 0f);
            tr.localScale = new Vector3(overlayScale.x, overlayScale.y, 1f);

            // ✅ 머티리얼 적용: sharedMaterial 직접 수정/오염 방지 위해 "타일별 인스턴스" 사용
            EnsureOverlayMaterialInstance();

            // ✅ Sorting Layer / Order 강제 적용
            // (빈 문자열이면 Default로 가니, 의도 없으면 Overlay로 두는 걸 추천)
            overlayRenderer.sortingLayerName = string.IsNullOrEmpty(overlaySortingLayerName)
                ? "Default"
                : overlaySortingLayerName;

            overlayRenderer.sortingOrder = overlayOrderInLayer;

            // 그림자 끄기
            overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
        }



        private void EnsureOverlayMaterialInstance()
        {
            // overlayMaterial이 없으면 기존 material을 그대로 쓰되, 가능하면 인스턴스화
            var baseMat = overlayMaterial != null ? overlayMaterial : overlayRenderer.sharedMaterial;

            if (baseMat == null)
            {
                // 마지막 안전장치: 기본 URP Unlit를 써도 됨 (프로젝트에 URP일 때)
                // URP가 아니면 null일 수 있으니 그냥 경고만.
                Debug.LogWarning($"[Tile] Overlay material is null on {name}. Assign a transparent material.");
                return;
            }

            // 타일마다 인스턴스 1개 만들어서 meshRenderer.material에 할당
            if (overlayMaterialInstance == null || overlayMaterialInstance.shader != baseMat.shader)
            {
                overlayMaterialInstance = new Material(baseMat);
                overlayRenderer.material = overlayMaterialInstance; // material = instance
            }
            else
            {
                // baseMat이 바뀐 경우 속성 갱신 (필요 시)
                // 완전 복사까지 원하면 CopyPropertiesFromMaterial 사용 가능
                // overlayMaterialInstance.CopyPropertiesFromMaterial(baseMat);
                overlayRenderer.material = overlayMaterialInstance;
            }

            // RenderQueue 옵션(타일별 인스턴스에만 적용)
            if (overlayRenderQueue != 0)
                overlayMaterialInstance.renderQueue = overlayRenderQueue;
        }

        // ✅ 인스펙터에서 정한 기본 색으로 오버레이 표시
        public void ShowOverlayDefault()
        {
            ShowOverlay(defaultOverlayColor);
        }

        // ✅ 외부에서 색을 주면 그 색으로 표시 (GridController에서 사용)
        public void ShowOverlay(Color color)
        {
            if (overlayRenderer == null) EnsureOverlay();
            if (overlayRenderer == null) return;

            // 인스펙터 변경 즉시 반영
            EnsureOverlay();

            overlayRenderer.enabled = true;

            // MaterialPropertyBlock로 색만 바꿈 (머티리얼 인스턴스 + MPB 둘 다 호환)
            overlayRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, color);
            mpb.SetColor(ColorId, color);
            overlayRenderer.SetPropertyBlock(mpb);
        }

        public void HideOverlay()
        {
            if (overlayRenderer != null)
                overlayRenderer.enabled = false;
        }

        public void Init(Vector2Int coord)
        {
            Coord = coord;
            name = $"Tile_{coord.x}_{coord.y}";
        }

        public void ResetBlocks()
        {
            TraversalBlockCount = 0;
            OccupancyBlockCount = 0;
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                mpb ??= new MaterialPropertyBlock();
                EnsureOverlay();
                if (overlayRenderer != null) overlayRenderer.enabled = false;
            }
        }
#endif
    }
}
