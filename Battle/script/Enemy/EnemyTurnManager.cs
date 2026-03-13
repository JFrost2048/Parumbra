using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid
{
    public class EnemyTurnManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridController grid;

        [Header("Tuning")]
        [SerializeField] private float perEnemyDelay = 0.05f;
        [SerializeField] private int defaultAttackKey = 1;

        public bool IsEnemyTurn { get; private set; }

        public void StartEnemyTurn()
        {
            if (IsEnemyTurn) return;
            if (grid == null)
            {
                Debug.LogError("[EnemyTurnManager] grid ref is null");
                return;
            }
            StartCoroutine(CoEnemyTurn());
        }

        private IEnumerator CoEnemyTurn()
        {
            IsEnemyTurn = true;

            // 플레이어 입력/상태 잠금
            grid.SetInputEnabled(false);
            grid.ClearSelectionAndOverlays();

            // 적/플레이어 유닛 수집
            var enemies = grid.GetUnitsByFaction(Faction.Enemy);
            var players = grid.GetUnitsByFaction(Faction.Player);

            // 적 유닛 AP 리필 및 턴 플래그 초기화
            foreach (var e in enemies)
            {
                if (e == null) continue;
                if (e.Health != null && e.Health.IsDead) continue;

                e.ResetTurnFlags();

                var res = e.GetComponent<UnitResources>();
                if (res != null)
                    res.RefillAP();
                else
                    Debug.LogWarning($"[ENEMY TURN] {e.name} has NO UnitResources (cannot spend AP)");
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (enemy.Health != null && enemy.Health.IsDead) continue;

                // 플레이어가 죽을 수 있으니 갱신
                players = grid.GetUnitsByFaction(Faction.Player);
                if (players.Count == 0) break;

                var target = FindClosestPlayer(enemy, players);
                Debug.Log($"[ENEMY] {enemy.name} at {enemy.Coord} -> target={(target ? target.name : "NULL")} at {(target ? target.Coord.ToString() : "NA")}");

                if (target == null) continue;

                // 1) 바로 공격 가능하면 공격
                if (grid.CanEnemyAttack(enemy, target, defaultAttackKey))
                {
                    Debug.Log($"[ENEMY] {enemy.name} CAN attack {target.name} using key={defaultAttackKey}");

                    bool ok = grid.TryEnemyAttack(enemy, target, defaultAttackKey);

                    Debug.Log(ok
                        ? $"[ENEMY] {enemy.name} ATTACK SUCCESS -> {target.name}"
                        : $"[ENEMY] {enemy.name} ATTACK FAILED -> {target.name}");

                    if (perEnemyDelay > 0f) yield return new WaitForSeconds(perEnemyDelay);
                    continue;
                }
                else
                {
                    Debug.Log($"[ENEMY] {enemy.name} cannot attack now (key={defaultAttackKey}), will try move");
                }


                // 2) 이동 가능한 타일 중 타겟에 가장 가까워지는 타일 선택
                if (TryPickBestMoveToward(enemy, target, out var bestMove))
                {
                    yield return grid.CoMoveUnit(enemy, bestMove);

                    // 3) 이동 후 공격 재시도
                    if (grid.CanEnemyAttack(enemy, target, defaultAttackKey))
                        grid.TryEnemyAttack(enemy, target, defaultAttackKey);
                }

                if (perEnemyDelay > 0f) yield return new WaitForSeconds(perEnemyDelay);
            }

            // 적 턴 종료 → 플레이어 턴 복귀
            grid.SetInputEnabled(true);
            grid.SelectFirstAlivePlayerUnit(); // 선택/오버레이 복구 용

            IsEnemyTurn = false;
        }

        private Unit FindClosestPlayer(Unit enemy, List<Unit> players)
        {
            Unit best = null;
            int bestDist = int.MaxValue;

            var e = enemy.Coord;

            foreach (var p in players)
            {
                if (p == null) continue;
                if (p.Health != null && p.Health.IsDead) continue;

                var c = p.Coord;
                int dist = Mathf.Abs(c.x - e.x) + Mathf.Abs(c.y - e.y); // Manhattan

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }
            return best;
        }

        private bool TryPickBestMoveToward(Unit enemy, Unit target, out Vector2Int best)
        {
            best = enemy.Coord;

            // GridController에서 “이 유닛이 이번 턴에 갈 수 있는 타일들” 얻기
            var dist = grid.GetMoveDist(enemy); // Dictionary<Vector2Int,int>
            if (dist == null || dist.Count == 0) return false;

            var targetC = target.Coord;

            int bestManhattan = int.MaxValue;
            bool found = false;

            foreach (var kv in dist)
            {
                var c = kv.Key;
                int cost = kv.Value;
                if (cost <= 0) continue;

                // 멈출 수 있어야 함(점유/BlocksOccupancy 등)
                if (!grid.CanStopAt(enemy, c)) continue;

                int d = Mathf.Abs(c.x - targetC.x) + Mathf.Abs(c.y - targetC.y);
                if (d < bestManhattan)
                {
                    bestManhattan = d;
                    best = c;
                    found = true;
                }
            }

            return found;
        }
    }
}