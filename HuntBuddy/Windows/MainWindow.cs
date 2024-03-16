﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using HuntBuddy.Utils;

using ImGuiNET;

namespace HuntBuddy.Windows;

/// <summary>
/// Main plugin window.
/// </summary>
public class MainWindow: Window {
	public MainWindow() : base(
		$"{Plugin.Instance.Name}",
		ImGuiWindowFlags.NoDocking,
		true) {
		this.Size = new Vector2(400 * ImGui.GetIO().FontGlobalScale, 500);
		this.SizeCondition = ImGuiCond.FirstUseEver;
	}

	public override void PreOpenCheck() {
		if (Plugin.Instance.Configuration.LockWindowPositions) {
			if (!this.Flags.HasFlag(ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove)) {
				this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
			}
		}
		else {
			this.Flags &= ~(ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
		}
	}

	public override unsafe void Draw() {
		if (!Plugin.Instance.MobHuntEntriesReady) {
			ImGui.Text("Reloading data ...");
			return;
		}

		if (InterfaceUtil.IconButton(FontAwesomeIcon.Redo, "Reload")) {
			Plugin.Instance.MobHuntEntriesReady = false;
			Task.Run(Plugin.Instance.ReloadData);
			return;
		}

		if (ImGui.IsItemHovered()) {
			ImGui.BeginTooltip();
			ImGui.Text("Click this button to reload daily hunt data");
			ImGui.EndTooltip();
		}

		ImGui.SameLine();

		if (InterfaceUtil.IconButton(FontAwesomeIcon.Cog, "Config")) {
			Plugin.Instance.OpenConfigUi();
		}

		IEnumerable<KeyValuePair<string, Dictionary<KeyValuePair<uint, string>, List<MobHuntEntry>>>> expansionEntriesWithTreeNodes = Plugin.Instance
			.MobHuntEntries
			.Where(expansionEntry => ImGui.TreeNode(expansionEntry.Key));
		foreach (KeyValuePair<string, Dictionary<KeyValuePair<uint, string>, List<MobHuntEntry>>> expansionEntry in expansionEntriesWithTreeNodes) {
			IEnumerable<KeyValuePair<KeyValuePair<uint, string>, List<MobHuntEntry>>> mobEntriesWithTreeNodes = expansionEntry.Value
				.Where(entry => {
					bool treeOpen = ImGui.TreeNodeEx(entry.Key.Value, ImGuiTreeNodeFlags.AllowItemOverlap);
					ImGui.SameLine();
					int killedCount = entry.Value.Count(x => Plugin.Instance.MobHuntStruct->CurrentKills[x.CurrentKillsOffset] == x.NeededKills);
					if (killedCount != entry.Value.Count) {
						ImGui.Text($"({killedCount}/{entry.Value.Count})");
					}
					else {
						ImGui.TextColored(
							new Vector4(0f, 1f, 0f, 1f),
							$"({killedCount}/{entry.Value.Count})");
					}
					return treeOpen;
				});
			foreach (KeyValuePair<KeyValuePair<uint, string>, List<MobHuntEntry>> entry in mobEntriesWithTreeNodes) {
				foreach (MobHuntEntry? mobHuntEntry in entry.Value) {
					if (Location.Database.ContainsKey(mobHuntEntry.MobHuntId)) {
						if (InterfaceUtil.IconButton(FontAwesomeIcon.MapMarkerAlt, $"pin##{mobHuntEntry.MobHuntId}")) {
							Location.CreateMapMarker(
								mobHuntEntry.TerritoryType,
								mobHuntEntry.MapId,
								mobHuntEntry.MobHuntId,
								mobHuntEntry.Name,
								Location.OpenType.None);
						}

						if (ImGui.IsItemHovered()) {
							ImGui.BeginTooltip();
							ImGui.Text("Place marker on the map");
							ImGui.EndTooltip();
						}

						ImGui.SameLine();

						if (InterfaceUtil.IconButton(FontAwesomeIcon.MapMarkedAlt, $"open##{mobHuntEntry.MobHuntId}")) {
							bool includeArea = Plugin.Instance.Configuration.IncludeAreaOnMap;
							if (ImGui.IsKeyDown(ImGuiKey.ModShift)) {
								includeArea = !includeArea;
							}

							Location.CreateMapMarker(
								mobHuntEntry.TerritoryType,
								mobHuntEntry.MapId,
								mobHuntEntry.MobHuntId,
								mobHuntEntry.Name,
								includeArea ? Location.OpenType.ShowOpen : Location.OpenType.MarkerOpen);
						}

						if (ImGui.IsItemHovered()) {
							Vector4 color = ImGui.IsKeyDown(ImGuiKey.ModShift)
								? new Vector4(0f, 0.7f, 0f, 1f)
								: new Vector4(0.7f, 0.7f, 0.7f, 1f);
							ImGui.BeginTooltip();
							if (Plugin.Instance.Configuration.IncludeAreaOnMap) {
								ImGui.Text("Show hunt area on the map");
								ImGui.TextColored(
									color,
									"Hold [SHIFT] to show the location only");
							}
							else {
								ImGui.Text("Show hunt location on the map");
								ImGui.TextColored(
									color,
									"Hold [SHIFT] to include the area");
							}

							ImGui.EndTooltip();
						}

						ImGui.SameLine();

						if (Plugin.TeleportConsumer?.IsAvailable == true) {
							if (InterfaceUtil.IconButton(FontAwesomeIcon.StreetView, $"teleport##{mobHuntEntry.MobHuntId}")) {
								Location.TeleportToNearestAetheryte(
									mobHuntEntry.TerritoryType,
									mobHuntEntry.MapId,
									mobHuntEntry.MobHuntId);
							}

							if (ImGui.IsItemHovered()) {
								ImGui.BeginTooltip();
								ImGui.Text("Teleport to nearest aetheryte");
								ImGui.EndTooltip();
							}

							ImGui.SameLine();
						}

						if (Plugin.Instance.Configuration.EnableXivEspIntegration && Plugin.EspConsumer?.IsAvailable == true) {
							if (InterfaceUtil.IconButton(FontAwesomeIcon.Search, $"esp##{mobHuntEntry.MobHuntId}")) {
								Plugin.EspConsumer.SearchFor(mobHuntEntry.Name!);
							}

							if (ImGui.IsItemHovered()) {
								ImGui.BeginTooltip();
								ImGui.Text("Set XivEsp search to this target");
								ImGui.EndTooltip();
							}

							ImGui.SameLine();
						}
					}

					int currentKills = Plugin.Instance.MobHuntStruct->CurrentKills[mobHuntEntry.CurrentKillsOffset];
					ImGui.Text(mobHuntEntry.Name);
					if (ImGui.IsItemHovered()) {
						ImGui.PushStyleColor(ImGuiCol.PopupBg, Vector4.Zero);
						ImGui.BeginTooltip();
						InterfaceUtil.DrawHuntIcon(mobHuntEntry);
						ImGui.PopStyleColor();
						ImGui.EndTooltip();
					}

					ImGui.SameLine();
					if (currentKills != mobHuntEntry.NeededKills) {
						ImGui.Text($"({currentKills}/{mobHuntEntry.NeededKills})");
					}
					else {
						ImGui.TextColored(
							new Vector4(0f, 1f, 0f, 1f),
							$"({currentKills}/{mobHuntEntry.NeededKills})");
					}
				}

				ImGui.TreePop();
			}

			ImGui.TreePop();
		}
	}
}
