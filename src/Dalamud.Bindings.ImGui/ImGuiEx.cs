using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Hypostasis;
using Hypostasis.Dalamud;
using Lumina.Excel;

namespace Dalamud.Bindings.ImGui;

public static class ImGuiEx
{
	public sealed class IDBlock : IDisposable
	{
		private static readonly IDBlock instance = new IDBlock();

		private IDBlock()
		{
		}

		public static IDBlock Begin(int id)
		{
			ImGui.PushID(id);
			return instance;
		}

		public static IDBlock Begin(uint id)
		{
			return Begin((int)id);
		}

		public static IDBlock Begin(nint id)
		{
			ImGui.PushID(id);
			return instance;
		}

		public static IDBlock Begin(nuint id)
		{
			return Begin((nint)id);
		}

		public static IDBlock Begin(string id)
		{
			ImGui.PushID(id);
			return instance;
		}

		public unsafe static IDBlock Begin(void* ptr)
		{
			ImGui.PushID(ptr);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopID();
		}
	}

	public sealed class StyleVarBlock : IDisposable
	{
		private static readonly StyleVarBlock instance = new StyleVarBlock();

		private StyleVarBlock()
		{
		}

		public static StyleVarBlock Begin(ImGuiStyleVar idx, float val, bool conditional = true)
		{
			if (!conditional)
			{
				return null;
			}
			ImGui.PushStyleVar(idx, val);
			return instance;
		}

		public static StyleVarBlock Begin(ImGuiStyleVar idx, Vector2 val, bool conditional = true)
		{
			if (!conditional)
			{
				return null;
			}
			ImGui.PushStyleVar(idx, val);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopStyleVar();
		}
	}

	public sealed class StyleColorBlock : IDisposable
	{
		private static readonly StyleColorBlock instance = new StyleColorBlock();

		private StyleColorBlock()
		{
		}

		public static StyleColorBlock Begin(ImGuiCol idx, uint val, bool conditional = true)
		{
			if (!conditional)
			{
				return null;
			}
			ImGui.PushStyleColor(idx, val);
			return instance;
		}

		public static StyleColorBlock Begin(ImGuiCol idx, Vector4 val, bool conditional = true)
		{
			if (!conditional)
			{
				return null;
			}
			ImGui.PushStyleColor(idx, val);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopStyleColor();
		}
	}

	public sealed class IndentBlock : IDisposable
	{
		private static readonly IndentBlock instance = new IndentBlock();

		private IndentBlock()
		{
		}

		public static IndentBlock Begin()
		{
			PushIndent();
			return instance;
		}

		public static IndentBlock Begin(float indent)
		{
			if (indent == 0f)
			{
				return null;
			}
			PushIndent(indent);
			return instance;
		}

		public void Dispose()
		{
			PopIndent();
		}
	}

	public sealed class FontBlock : IDisposable
	{
		private static readonly FontBlock instance = new FontBlock();

		private FontBlock()
		{
		}

		public static FontBlock Begin(ImFontPtr font)
		{
			ImGui.PushFont(font);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopFont();
		}
	}

	public sealed class GroupBlock : IDisposable
	{
		private static readonly GroupBlock instance = new GroupBlock();

		private GroupBlock()
		{
		}

		public static GroupBlock Begin()
		{
			ImGui.BeginGroup();
			return instance;
		}

		public void Dispose()
		{
			ImGui.EndGroup();
		}
	}

	public sealed class ClipRectBlock : IDisposable
	{
		private static readonly ClipRectBlock instance = new ClipRectBlock();

		private ClipRectBlock()
		{
		}

		public static ClipRectBlock Begin(Vector2 min, Vector2 max, bool overlap = true)
		{
			ImGui.PushClipRect(min, max, overlap);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopClipRect();
		}
	}

	public sealed class TooltipBlock : IDisposable
	{
		private static readonly TooltipBlock instance = new TooltipBlock();

		private TooltipBlock()
		{
		}

		public static TooltipBlock Begin()
		{
			ImGui.BeginTooltip();
			return instance;
		}

		public void Dispose()
		{
			ImGui.EndTooltip();
		}
	}

	public sealed class DisabledBlock : IDisposable
	{
		private static readonly DisabledBlock instance = new DisabledBlock();

		private DisabledBlock()
		{
		}

		public static DisabledBlock Begin(bool conditional = true)
		{
			ImGui.BeginDisabled(conditional);
			return instance;
		}

		public void Dispose()
		{
			ImGui.EndDisabled();
		}
	}

	public sealed class AllowKeyboardFocusBlock : IDisposable
	{
		private static readonly AllowKeyboardFocusBlock instance = new AllowKeyboardFocusBlock();

		private AllowKeyboardFocusBlock()
		{
		}

		public static AllowKeyboardFocusBlock Begin(bool allow = false)
		{
			ImGui.PushAllowKeyboardFocus(allow);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopAllowKeyboardFocus();
		}
	}

	public sealed class ButtonRepeatBlock : IDisposable
	{
		private static readonly ButtonRepeatBlock instance = new ButtonRepeatBlock();

		private ButtonRepeatBlock()
		{
		}

		public static ButtonRepeatBlock Begin(bool repeat = true)
		{
			ImGui.PushButtonRepeat(repeat);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopButtonRepeat();
		}
	}

	public sealed class ItemWidthBlock : IDisposable
	{
		private static readonly ItemWidthBlock instance = new ItemWidthBlock();

		private ItemWidthBlock()
		{
		}

		public static ItemWidthBlock Begin(float width)
		{
			ImGui.PushItemWidth(width);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopItemWidth();
		}
	}

	public sealed class TextWrapPosBlock : IDisposable
	{
		private static readonly TextWrapPosBlock instance = new TextWrapPosBlock();

		private TextWrapPosBlock()
		{
		}

		public static TextWrapPosBlock Begin()
		{
			ImGui.PushTextWrapPos();
			return instance;
		}

		public static TextWrapPosBlock Begin(float posX)
		{
			ImGui.PushTextWrapPos(posX);
			return instance;
		}

		public void Dispose()
		{
			ImGui.PopTextWrapPos();
		}
	}

	public record ExcelSheetOptions<T> where T : struct, IExcelRow<T>
	{
		public Func<T, string> FormatRow { get; init; }

		public Func<T, string, bool> SearchPredicate { get; init; }

		public Func<T, bool, bool> DrawSelectable { get; init; }

		public IEnumerable<T> FilteredSheet { get; init; }

		public Vector2? Size { get; init; }
	}

	public record ExcelSheetComboOptions<T> : ExcelSheetOptions<T> where T : struct, IExcelRow<T>
	{
		public Func<T, string> GetPreview { get; init; }

		public ImGuiComboFlags ComboFlags { get; init; }
	}

	public record ExcelSheetPopupOptions<T> : ExcelSheetOptions<T> where T : struct, IExcelRow<T>
	{
		public ImGuiPopupFlags PopupFlags { get; init; }

		public bool CloseOnSelection { get; init; }

		public Func<T, bool> IsRowSelected { get; init; }
	}

	public record GroupBoxOptions
	{
		public bool Collapsible { get; init; } = false;

		public uint HeaderTextColor { get; init; } = ImGui.GetColorU32(ImGuiCol.Text);

		public Action HeaderTextAction { get; init; } = null;

		public uint BorderColor { get; init; } = ImGui.GetColorU32(ImGuiCol.Border);

		public Vector2 BorderPadding { get; init; } = ImGui.GetStyle().WindowPadding;

		public float BorderRounding { get; init; } = ImGui.GetStyle().FrameRounding;

		public ImDrawFlags DrawFlags { get; init; } = ImDrawFlags.None;

		public float BorderThickness { get; init; } = 2f;

		public float Width { get; set; }

		public float MaxX { get; set; }
	}

	public record HeaderIconOptions
	{
		public Vector2 Offset { get; init; } = Vector2.Zero;

		public ImGuiMouseButton MouseButton { get; init; } = ImGuiMouseButton.Left;

		public string Tooltip { get; init; } = string.Empty;

		public uint Color { get; init; } = uint.MaxValue;

		public bool ToastTooltipOnClick { get; init; } = false;

		public ImGuiMouseButton ToastTooltipOnClickButton { get; init; } = ImGuiMouseButton.Left;
	}

	public class ListClipper : IEnumerable<(int, int)>, IEnumerable, IDisposable
	{
		private ImGuiListClipperPtr clipper;

		private readonly int rows;

		private readonly int columns;

		private readonly bool twoDimensional;

		private readonly int itemRemainder;

		public int FirstRow { get; private set; } = -1;

		public int LastRow => CurrentRow;

		public int CurrentRow { get; private set; }

		public bool IsStepped => CurrentRow == DisplayStart;

		public int DisplayStart => clipper.DisplayStart;

		public int DisplayEnd => clipper.DisplayEnd;

		public IEnumerable<int> Rows
		{
			get
			{
				while (clipper.Step())
				{
					if (clipper.ItemsHeight > 0f && FirstRow < 0)
					{
						FirstRow = (int)(ImGui.GetScrollY() / clipper.ItemsHeight);
					}
					for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
					{
						CurrentRow = i;
						yield return twoDimensional ? i : (i * columns);
					}
				}
			}
		}

		public IEnumerable<int> Columns
		{
			get
			{
				int cols = ((itemRemainder == 0 || rows != DisplayEnd || CurrentRow != DisplayEnd - 1) ? columns : itemRemainder);
				for (int j = 0; j < cols; j++)
				{
					yield return j;
				}
			}
		}

		public unsafe ListClipper(int items, int cols = 1, bool twoD = false, float itemHeight = 0f)
		{
			twoDimensional = twoD;
			columns = cols;
			rows = (twoDimensional ? items : ((int)MathF.Ceiling((float)items / (float)columns)));
			itemRemainder = ((!twoDimensional) ? (items % columns) : 0);
			clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
			clipper.Begin(rows, itemHeight);
		}

		public IEnumerator<(int, int)> GetEnumerator()
		{
			return (from i in Rows
				from j in Columns
				select (i: i, j: j)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Dispose()
		{
			clipper.Destroy();
			GC.SuppressFinalize(this);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass29_0<T> where T : struct, IExcelRow<T>
	{
		public PropertyInfo[] properties;

		public ImGuiTableSortSpecsPtr sortSpecs;

		internal bool _003CExcelSheetTable_003Eb__3(T row)
		{
			return row.RowId.ToString() == tableSearchText || _003CExcelSheetTable_003Eg__GetPropertiesAsStrings_007C29_1<T>(properties, row).Any((string valueStr) => valueStr.Contains(tableSearchText, StringComparison.CurrentCultureIgnoreCase));
		}

		internal IComparable _003CExcelSheetTable_003Eb__4(T row)
		{
			return _003CExcelSheetTable_003Eg__GetComparable_007C29_2<T>(properties[sortSpecs.Specs.ColumnIndex - 1].GetValue(row));
		}

		internal IComparable _003CExcelSheetTable_003Eb__5(T row)
		{
			return _003CExcelSheetTable_003Eg__GetComparable_007C29_2<T>(properties[sortSpecs.Specs.ColumnIndex - 1].GetValue(row));
		}
	}

	private static string sheetSearchText;

	private static uint[] filteredSearchIDs;

	private static string prevSearchID;

	private static Type prevSearchType;

	private static string tableSearchText = string.Empty;

	private static uint[] filteredTableSearchSheetIDs;

	private static string prevTableSearchID;

	private static Type prevTableSearchType;

	private static bool tableCompatMode = false;

	private static readonly Stack<GroupBoxOptions> groupBoxOptionsStack = new Stack<GroupBoxOptions>();

	private static uint headerLastWindowID = 0u;

	private static ulong headerLastFrame = 0uL;

	private static uint headerCurrentPos = 0u;

	private static float headerImGuiButtonWidth = 0f;

	private static readonly Stack<float> fontScaleStack = new Stack<float>();

	private static float curScale = 1f;

	private static readonly Stack<float> indentStack = new Stack<float>();

	private static object dragID = null;

	private static bool isDraggingItem = false;

	private static bool initialPosition = false;

	private static Vector2 lastGridPosition = Vector2.Zero;

	private static Vector2 gridCenter = Vector2.Zero;

	private static void ExcelSheetSearchInput<T>(string id, IEnumerable<T> filteredSheet, Func<T, string, bool> searchPredicate) where T : struct, IExcelRow<T>
	{
		if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
		{
			if (id != prevSearchID)
			{
				if (typeof(T) != prevSearchType)
				{
					sheetSearchText = string.Empty;
					prevSearchType = typeof(T);
				}
				filteredSearchIDs = null;
				prevSearchID = id;
			}
			ImGui.SetKeyboardFocusHere(0);
		}
		if (ImGui.InputTextWithHint("##ExcelSheetSearch", "Search", ref sheetSearchText, 128, ImGuiInputTextFlags.AutoSelectAll))
		{
			filteredSearchIDs = null;
		}
		if (filteredSearchIDs == null)
		{
			filteredSearchIDs = (from r in filteredSheet
				where searchPredicate(r, sheetSearchText)
				select r.RowId).ToArray();
		}
	}

	public static bool ExcelSheetCombo<T>(string id, ref uint selectedRow, ExcelSheetComboOptions<T> options = null) where T : struct, IExcelRow<T>
	{
		if ((object)options == null)
		{
			options = new ExcelSheetComboOptions<T>();
		}
		ExcelSheet<T> excelSheet = DalamudApi.DataManager.GetExcelSheet<T>();
		Func<T, string> func = options.GetPreview ?? options.FormatRow;
		ImU8String label = id;
		T? rowOrDefault = excelSheet.GetRowOrDefault(selectedRow);
		string text;
		if (rowOrDefault.HasValue)
		{
			T valueOrDefault = rowOrDefault.GetValueOrDefault();
			text = func(valueOrDefault);
		}
		else
		{
			text = selectedRow.ToString();
		}
		if (!ImGui.BeginCombo(label, text, options.ComboFlags | ImGuiComboFlags.HeightLargest))
		{
			return false;
		}
		ExcelSheetSearchInput(id, options.FilteredSheet ?? excelSheet, options.SearchPredicate ?? ((Func<T, string, bool>)((T row, string s) => options.FormatRow(row).Contains(s, StringComparison.CurrentCultureIgnoreCase))));
		ImGui.BeginChild("ExcelSheetSearchList", options.Size ?? new Vector2(0f, 200f * ImGuiHelpers.GlobalScale), border: true);
		bool flag = false;
		Func<T, bool, bool> func2 = options.DrawSelectable ?? ((Func<T, bool, bool>)((T row, bool selected) => ImGui.Selectable(options.FormatRow(row), selected)));
		using (ListClipper listClipper = new ListClipper(filteredSearchIDs.Length))
		{
			foreach (int row in listClipper.Rows)
			{
				rowOrDefault = excelSheet.GetRowOrDefault(filteredSearchIDs[row]);
				if (!rowOrDefault.HasValue)
				{
					continue;
				}
				T valueOrDefault2 = rowOrDefault.GetValueOrDefault();
				if (1 == 0)
				{
					continue;
				}
				using (IDBlock.Begin(row))
				{
					if (!func2(valueOrDefault2, selectedRow == valueOrDefault2.RowId))
					{
						continue;
					}
					selectedRow = valueOrDefault2.RowId;
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndChild();
		ImGui.EndCombo();
		return flag;
	}

	public static bool ExcelSheetPopup<T>(string id, out uint selectedRow, ExcelSheetPopupOptions<T> options = null) where T : struct, IExcelRow<T>
	{
		if ((object)options == null)
		{
			options = new ExcelSheetPopupOptions<T>();
		}
		ExcelSheet<T> excelSheet = DalamudApi.DataManager.GetExcelSheet<T>();
		selectedRow = 0u;
		ImGui.SetNextWindowSize(options.Size ?? new Vector2(0f, 250f * ImGuiHelpers.GlobalScale));
		if (!ImGui.BeginPopupContextItem(id, options.PopupFlags))
		{
			return false;
		}
		ExcelSheetSearchInput(id, options.FilteredSheet ?? excelSheet, options.SearchPredicate ?? ((Func<T, string, bool>)((T row, string s) => options.FormatRow(row).Contains(s, StringComparison.CurrentCultureIgnoreCase))));
		ImGui.BeginChild("ExcelSheetSearchList", Vector2.Zero, border: true);
		bool flag = false;
		Func<T, bool, bool> func = options.DrawSelectable ?? ((Func<T, bool, bool>)((T row, bool selected) => ImGui.Selectable(options.FormatRow(row), selected)));
		using (ListClipper listClipper = new ListClipper(filteredSearchIDs.Length))
		{
			foreach (int row in listClipper.Rows)
			{
				T? rowOrDefault = excelSheet.GetRowOrDefault(filteredSearchIDs[row]);
				if (!rowOrDefault.HasValue)
				{
					continue;
				}
				T valueOrDefault = rowOrDefault.GetValueOrDefault();
				if (1 == 0)
				{
					continue;
				}
				using (IDBlock.Begin(row))
				{
					if (func(valueOrDefault, options.IsRowSelected(valueOrDefault)))
					{
						selectedRow = valueOrDefault.RowId;
						flag = true;
					}
				}
			}
		}
		if (flag && options.CloseOnSelection)
		{
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndChild();
		ImGui.EndPopup();
		return flag;
	}

	public static bool ExcelSheetMultiselectPopup<T>(string id, ICollection<uint> selectedRows, ExcelSheetPopupOptions<T> options = null) where T : struct, IExcelRow<T>
	{
		if ((object)options == null)
		{
			options = new ExcelSheetPopupOptions<T>();
		}
		options = options with
		{
			IsRowSelected = (T row) => selectedRows.Contains(row.RowId)
		};
		if (!ExcelSheetPopup(id, out var selectedRow, options))
		{
			return false;
		}
		if (!selectedRows.Remove(selectedRow))
		{
			selectedRows.Add(selectedRow);
		}
		return true;
	}

	public static void ExcelSheetTable<T>(string id) where T : struct, IExcelRow<T>
	{
		_003C_003Ec__DisplayClass29_0<T> obj = new _003C_003Ec__DisplayClass29_0<T>();
	}

	public static bool BeginGroupBox(string id = null, float minimumWindowPercent = 1f, GroupBoxOptions options = null)
	{
		if ((object)options == null)
		{
			options = new GroupBoxOptions();
		}
		ImGui.BeginGroup();
		bool flag = true;
		if (!string.IsNullOrEmpty(id))
		{
			if (!options.Collapsible)
			{
				ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(options.HeaderTextColor), id);
			}
			else
			{
				ImGui.PushStyleColor(ImGuiCol.Text, options.HeaderTextColor);
				flag = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.DefaultOpen);
				ImGui.PopStyleColor();
			}
			options.HeaderTextAction?.Invoke();
			ImGui.Indent();
			ImGui.Unindent();
		}
		ImGuiStylePtr style = ImGui.GetStyle();
		float num = style.ItemSpacing.X * (1f - minimumWindowPercent);
		float num2 = (groupBoxOptionsStack.TryPeek(out GroupBoxOptions result) ? (result.Width - result.BorderPadding.X * 2f) : (ImGui.GetWindowContentRegionMax().X - style.WindowPadding.X));
		float num3 = Math.Max(num2 * minimumWindowPercent - num, 1f);
		options.Width = ((minimumWindowPercent > 0f) ? num3 : 0f);
		ImGui.BeginGroup();
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
		Vector2 borderPadding = options.BorderPadding;
		borderPadding.X = num3;
		ImGui.Dummy(borderPadding);
		ImGui.PopStyleVar();
		Vector2 itemRectMax = ImGui.GetItemRectMax();
		options.MaxX = itemRectMax.X;
		if (options.Width > 0f)
		{
			Vector2 itemRectMin = ImGui.GetItemRectMin();
			borderPadding = itemRectMax;
			borderPadding.Y = 10000f;
			ImGui.PushClipRect(itemRectMin, borderPadding, intersectWithCurrentClipRect: true);
		}
		ImGui.Indent(Math.Max(options.BorderPadding.X, 0.01f));
		ImGui.PushItemWidth(MathF.Floor((num3 - options.BorderPadding.X * 2f) * 0.65f));
		groupBoxOptionsStack.Push(options);
		if (flag)
		{
			return true;
		}
		ImGui.TextDisabled(". . .");
		EndGroupBox();
		return false;
	}

	public static bool BeginGroupBox(string text, GroupBoxOptions options)
	{
		return BeginGroupBox(text, 1f, options);
	}

	public static bool BeginGroupBox(uint borderColor, float minimumWindowPercent = 1f)
	{
		return BeginGroupBox(null, minimumWindowPercent, new GroupBoxOptions
		{
			BorderColor = borderColor
		});
	}

	public static void EndGroupBox()
	{
		GroupBoxOptions groupBoxOptions = groupBoxOptionsStack.Pop();
		bool flag = groupBoxOptions.Width <= 0f;
		ImGui.PopItemWidth();
		ImGui.Unindent(Math.Max(groupBoxOptions.BorderPadding.X, 0.01f));
		if (!flag)
		{
			ImGui.PopClipRect();
		}
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
		Vector2 borderPadding = groupBoxOptions.BorderPadding;
		borderPadding.X = 0f;
		ImGui.Dummy(borderPadding);
		if (!flag)
		{
			ImGuiWindowPtr currentWindow = ImGuiP.GetCurrentWindow();
			ref ImGuiWindowTempData dC = ref currentWindow.DC;
			borderPadding = currentWindow.DC.CursorMaxPos;
			borderPadding.X = groupBoxOptions.MaxX;
			dC.CursorMaxPos = borderPadding;
		}
		ImGui.EndGroup();
		Vector2 itemRectMin = ImGui.GetItemRectMin();
		Vector2 vector;
		if (!flag)
		{
			borderPadding = ImGui.GetItemRectMax();
			borderPadding.X = groupBoxOptions.MaxX;
			vector = borderPadding;
		}
		else
		{
			vector = ImGui.GetItemRectMax();
		}
		Vector2 pMax = vector;
		ImGui.GetWindowDrawList().AddRect(itemRectMin, pMax, groupBoxOptions.BorderColor, groupBoxOptions.BorderRounding, groupBoxOptions.DrawFlags, groupBoxOptions.BorderThickness);
		ImGui.EndGroup();
	}

	public static bool AddHeaderIcon(string id, FontAwesomeIcon icon, HeaderIconOptions options = null)
	{
		if (ImGui.IsWindowCollapsed())
		{
			return false;
		}
		float globalScale = ImGuiHelpers.GlobalScale;
		uint iD = ImGui.GetID((nint)0);
		if (iD != headerLastWindowID || headerLastFrame != DalamudApi.PluginInterface.UiBuilder.FrameCount)
		{
			headerLastWindowID = iD;
			headerLastFrame = DalamudApi.PluginInterface.UiBuilder.FrameCount;
			headerCurrentPos = 0u;
			headerImGuiButtonWidth = 0f;
			ImGuiWindowPtr currentWindow = ImGuiP.GetCurrentWindow();
			if (currentWindow.HasCloseButton)
			{
				headerImGuiButtonWidth += 17f * globalScale;
			}
			if (!currentWindow.Flags.HasFlag(ImGuiWindowFlags.NoCollapse))
			{
				headerImGuiButtonWidth += 17f * globalScale;
			}
		}
		if ((object)options == null)
		{
			options = new HeaderIconOptions();
		}
		Vector2 cursorPos = ImGui.GetCursorPos();
		Vector2 size = new Vector2(20f * globalScale);
		Vector2 cursorPos2 = new Vector2(ImGui.GetWindowWidth() - size.X - headerImGuiButtonWidth - (float)(20 * headerCurrentPos++) * globalScale - ImGui.GetStyle().FramePadding.X * 2f, ImGui.GetScrollY() + 1f);
		ImGui.SetCursorPos(cursorPos2);
		ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
		windowDrawList.PushClipRectFullScreen();
		bool result = false;
		ImGui.InvisibleButton(id, size);
		Vector2 itemRectMin = ImGui.GetItemRectMin();
		Vector2 itemRectMax = ImGui.GetItemRectMax();
		Vector2 vector = ImGui.GetItemRectSize() / 2f;
		Vector2 center = itemRectMin + vector;
		if (ImGui.IsWindowHovered() && ImGui.IsMouseHoveringRect(itemRectMin, itemRectMax, clip: false))
		{
			if (!string.IsNullOrEmpty(options.Tooltip))
			{
				ImGui.SetTooltip(options.Tooltip);
			}
			ImGui.GetWindowDrawList().AddCircleFilled(center, vector.X, ImGui.GetColorU32(ImGui.IsMouseDown(ImGuiMouseButton.Left) ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered));
			if (ImGui.IsMouseReleased(options.MouseButton))
			{
				result = true;
			}
			if (options.ToastTooltipOnClick && ImGui.IsMouseReleased(options.ToastTooltipOnClickButton))
			{
				DalamudApi.NotificationManager.AddNotification(new Notification
				{
					Type = NotificationType.Info,
					Content = options.Tooltip
				});
			}
		}
		ImGui.SetCursorPos(cursorPos2);
		ImGui.PushFont(UiBuilder.IconFont);
		string text = icon.ToIconString();
		windowDrawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), itemRectMin + vector - ImGui.CalcTextSize(text) / 2f + options.Offset, options.Color, text);
		ImGui.PopFont();
		ImGui.PopClipRect();
		ImGui.SetCursorPos(cursorPos);
		return result;
	}

	public static void AddDonationHeader(string link = "https://ko-fi.com/unknownx7")
	{
		if (AddHeaderIcon("_DONATE", FontAwesomeIcon.Heart, new HeaderIconOptions
		{
			Color = 4281348304u,
			MouseButton = ImGuiMouseButton.Right,
			Tooltip = "\ue052 Right click to go to " + link,
			ToastTooltipOnClick = true
		}))
		{
			Util.StartProcess(link);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetItemTooltip(string s, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
	{
		if (ImGui.IsItemHovered(flags))
		{
			ImGui.SetTooltip(s);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsItemDoubleClicked(ImGuiMouseButton button = ImGuiMouseButton.Left, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
	{
		return ImGui.IsMouseDoubleClicked(button) && ImGui.IsItemHovered(flags);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsItemReleased(ImGuiMouseButton button = ImGuiMouseButton.Left, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
	{
		return ImGui.IsMouseReleased(button) && ImGui.IsItemHovered(flags);
	}

	public static void PushFontScale(float scale)
	{
		ImGui.SetWindowFontScale(scale);
		fontScaleStack.Push(curScale);
		curScale = scale;
	}

	public static void PopFontScale()
	{
		curScale = fontScaleStack.Pop();
		ImGui.SetWindowFontScale(curScale);
	}

	public static void PushFontSize(float size)
	{
		PushFontScale(size / ImGui.GetFont().FontSize);
	}

	public static void PopFontSize()
	{
		PopFontScale();
	}

	public static float GetFontScale()
	{
		return curScale;
	}

	public static float GetFontSize()
	{
		return curScale * ImGui.GetFont().FontSize;
	}

	public static void PushIndent(float indent = 0f)
	{
		ImGui.Indent(indent);
		indentStack.Push(indent);
	}

	public static void PopIndent()
	{
		ImGui.Unindent(indentStack.Pop());
	}

	public static void ClampWindowPosToViewport()
	{
		ImGuiViewportPtr windowViewport = ImGui.GetWindowViewport();
		if (!ImGui.IsWindowAppearing() && windowViewport.ID == ImGuiHelpers.MainViewport.ID)
		{
			Vector2 pos = windowViewport.Pos;
			ClampWindowPos(pos, pos + windowViewport.Size);
		}
	}

	public static void ClampWindowPos(Vector2 max)
	{
		ClampWindowPos(Vector2.Zero, max);
	}

	public static void ClampWindowPos(Vector2 min, Vector2 max)
	{
		Vector2 windowPos = ImGui.GetWindowPos();
		Vector2 windowSize = ImGui.GetWindowSize();
		float x = Math.Min(Math.Max(windowPos.X, min.X), max.X - windowSize.X);
		float y = Math.Min(Math.Max(windowPos.Y, min.Y), max.Y - windowSize.Y);
		ImGui.SetWindowPos(new Vector2(x, y));
	}

	public static bool IsWindowInMainViewport()
	{
		return ImGui.GetWindowViewport().ID == ImGuiHelpers.MainViewport.ID;
	}

	public static bool ShouldDrawInViewport()
	{
		return IsWindowInMainViewport() || Util.IsWindowFocused;
	}

	public static void ShouldDrawInViewport(out bool b)
	{
		b = ShouldDrawInViewport();
	}

	public static bool SetBoolOnGameFocus(ref bool b)
	{
		if (!b)
		{
			b = Util.IsWindowFocused;
		}
		return b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetClipboardTextOrDefault(string def = "")
	{
		try
		{
			return ImGui.GetClipboardText();
		}
		catch
		{
			return def;
		}
	}

	public static void PushClipRectFullScreen()
	{
		ImGui.GetWindowDrawList().PushClipRectFullScreen();
	}

	public static void TextCopyable(string text)
	{
		ImGui.TextUnformatted(text);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (ImGui.IsItemClicked())
			{
				ImGui.SetClipboardText(text);
			}
		}
	}

	public static void TextCopyable(Vector4 color, string text)
	{
		ImGui.TextColored(in color, text);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (ImGui.IsItemClicked())
			{
				ImGui.SetClipboardText(text);
			}
		}
	}

	public static void TextMarquee(string text, float speed = 0.1f)
	{
		float x = ImGui.CalcTextSize(text).X;
		float num = ImGui.GetContentRegionMax().X + x;
		float indentW = (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds * (num * speed) % num - x;
		ImGui.Indent(indentW);
		ImGui.TextUnformatted(text);
		ImGui.Unindent(indentW);
	}

	public static void TextMarquee(Vector4 color, string text, float speed = 0.1f)
	{
		float x = ImGui.CalcTextSize(text).X;
		float num = ImGui.GetContentRegionMax().X + x;
		float indentW = (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds * (num * speed) % num - x;
		ImGui.Indent(indentW);
		ImGui.TextColored(in color, text);
		ImGui.Unindent(indentW);
	}

	public static bool FontButton(string label, ImFontPtr font)
	{
		ImGui.PushFont(font);
		bool result = ImGui.Button(label);
		ImGui.PopFont();
		return result;
	}

	public static bool FontButton(string label, ImFontPtr font, Vector2 size)
	{
		ImGui.PushFont(font);
		bool result = ImGui.Button(label, size);
		ImGui.PopFont();
		return result;
	}

	public static bool DeleteConfirmationButton(Vector2 size = default(Vector2))
	{
		using (FontBlock.Begin(UiBuilder.IconFont))
		{
			ImGui.Button(FontAwesomeIcon.Times.ToIconString(), size);
			if (IsItemReleased(ImGuiMouseButton.Right))
			{
				return true;
			}
			using (StyleVarBlock.Begin(ImGuiStyleVar.PopupBorderSize, 1f))
			{
				if (!ImGui.BeginPopupContextItem(ImU8String.Empty, ImGuiPopupFlags.None))
				{
					return false;
				}
				bool result = ImGui.Selectable(FontAwesomeIcon.TrashAlt.ToIconString());
				ImGui.EndPopup();
				return result;
			}
		}
	}

	public static void BlockWindowDrag()
	{
		ImGuiIOPtr io = ImGui.GetIO();
		bool prev = io.ConfigWindowsMoveFromTitleBarOnly;
		io.ConfigWindowsMoveFromTitleBarOnly = true;
		DalamudApi.Framework.RunOnTick(() => io.ConfigWindowsMoveFromTitleBarOnly = prev);
	}

	private static void AddTextCentered(Vector2 pos, string text, uint color)
	{
		Vector2 vector = ImGui.CalcTextSize(text);
		ImGui.GetWindowDrawList().AddText(pos - vector / 2f, color, text);
	}

	public static void Prefix(string prefix = "◇")
	{
		Vector2 vector = new Vector2(ImGui.GetFrameHeight());
		ImGui.Dummy(vector);
		AddTextCentered(ImGui.GetItemRectMin() + vector / 2f, prefix, ImGui.GetColorU32(ImGuiCol.Text));
		ImGui.SameLine();
	}

	public static void Prefix(bool isLast)
	{
		Prefix(isLast ? "└" : "├");
	}

	public static bool RadioBox(string label, ref int v, string[] optionsArray, bool vertical)
	{
		if (!BeginGroupBox(label, 0f))
		{
			return false;
		}
		bool flag = false;
		int num = optionsArray.Length;
		float num2 = 0f;
		ImGui.PushID(label);
		for (int i = 0; i < num; i++)
		{
			string text = optionsArray[i];
			bool flag2 = v == i;
			flag |= ImGui.RadioButton(vertical ? text : $"##{i}", ref v, i) && !flag2;
			float x = ImGui.GetItemRectSize().X;
			num2 = Math.Max(x, num2);
			if (i == num - 1)
			{
				num2 -= x;
			}
			if (!vertical)
			{
				SetItemTooltip(text);
				if (i != num - 1)
				{
					ImGui.SameLine();
				}
			}
		}
		ImGui.PopID();
		if (vertical)
		{
			ImGui.SameLine();
			ImGui.Dummy(new Vector2(num2, 0f));
		}
		else if (v >= 0 && v < num)
		{
			ImGui.SameLine();
			string text2 = optionsArray[v];
			ImGui.TextUnformatted(text2);
			ImGui.SameLine();
			ImGui.Dummy(new Vector2(optionsArray.Select((string s) => ImGui.CalcTextSize(s).X).Max() - ImGui.CalcTextSize(text2).X, 0f));
		}
		EndGroupBox();
		return flag;
	}

	public static bool RadioBox(string label, ref int v, string options, bool vertical)
	{
		return RadioBox(label, ref v, options.Split('\0'), vertical);
	}

	public static bool RadioBox<T>(string label, ref T e, bool vertical) where T : struct, Enum
	{
		string[] names = Enum.GetNames<T>();
		int v = Array.IndexOf(names, Enum.GetName(e));
		bool flag = RadioBox(label, ref v, names.Select((string name) =>
		{
			FieldInfo? field = typeof(T).GetField(name);
			return (((object)field == null) ? null : field.GetCustomAttribute<DisplayAttribute>()?.Name) ?? name;
		}).ToArray(), vertical);
		if (flag)
		{
			e = Enum.Parse<T>(names[v]);
		}
		return flag;
	}

	public static bool RadioBox<T>(string label, ref T e, T[] optionsArray, bool vertical) where T : struct, Enum
	{
		int v = Array.IndexOf(optionsArray, e);
		bool flag = RadioBox(label, ref v, optionsArray.Select(Util.GetDisplayName).ToArray(), vertical);
		if (flag)
		{
			e = optionsArray[v];
		}
		return flag;
	}

	public static bool EnumCombo<T>(string label, ref T e, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum
	{
		if (!ImGui.BeginCombo(label, e.GetDisplayName(), flags))
		{
			return false;
		}
		string[] names = Enum.GetNames<T>();
		string name = Enum.GetName(e);
		string[] array = names;
		foreach (string text in array)
		{
			ImU8String label2 = new ImU8String(2, 2);
			FieldInfo? field = typeof(T).GetField(text);
			label2.AppendFormatted((((object)field == null) ? null : field.GetCustomAttribute<DisplayAttribute>()?.Name) ?? text);
			label2.AppendLiteral("##");
			label2.AppendFormatted(text);
			if (ImGui.Selectable(label2, text == name))
			{
				e = Enum.Parse<T>(text);
				ImGui.EndCombo();
				return true;
			}
		}
		ImGui.EndCombo();
		return false;
	}

	public static bool CheckboxTristate(string label, ref bool? v)
	{
		bool flag;
		if (!v.HasValue)
		{
			bool v2 = false;
			flag = ImGui.Checkbox(label, ref v2);
			if (flag)
			{
				v = true;
			}
			float frameHeight = ImGui.GetFrameHeight();
			float value = Math.Max(MathF.Floor(frameHeight / 4f), 1f);
			Vector2 vector = new Vector2(value);
			Vector2 itemRectMin = ImGui.GetItemRectMin();
			Vector2 vector2 = itemRectMin + new Vector2(frameHeight);
			ImGui.GetWindowDrawList().AddRect(itemRectMin + vector, vector2 - vector, ImGui.GetColorU32(ImGuiCol.CheckMark), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, 3f * ImGuiHelpers.GlobalScale);
		}
		else
		{
			bool v3 = v.Value;
			bool flag2 = !v3;
			if (flag2)
			{
				ImGui.PushStyleColor(ImGuiCol.CheckMark, Vector4.Zero);
			}
			flag = ImGui.Checkbox(label, ref v3);
			if (flag)
			{
				v = (v3 ? ((bool?)null) : new bool?(false));
			}
			if (flag2)
			{
				ImGui.PopStyleColor();
			}
		}
		return flag;
	}

	public static void FloatingDrawable(Action<ImDrawListPtr, float, Vector2> draw, uint timerMS = 1000u)
	{
		ImGuiViewportPtr windowViewport = ImGui.GetWindowViewport();
		ImGuiViewportPtr viewport = ((!windowViewport.IsNull) ? windowViewport : ImGui.GetMainViewport());
		Vector2 pos = ImGui.GetMousePos();
		Stopwatch timer = Stopwatch.StartNew();
		DalamudApi.PluginInterface.UiBuilder.Draw += f;
		void f()
		{
			float num = Math.Min((float)timer.ElapsedMilliseconds / (float)timerMS, 1f);
			if (num < 1f && !viewport.Flags.HasFlag(ImGuiViewportFlags.NoTaskBarIcon))
			{
				draw(ImGui.GetForegroundDrawList(viewport), num, pos);
			}
			else
			{
				DalamudApi.PluginInterface.UiBuilder.Draw -= f;
			}
		}
	}

	public static void FloatingText(string text, uint color = uint.MaxValue, uint timerMS = 1000u)
	{
		Vector2 textSize = ImGui.CalcTextSize(text);
		uint startingAlpha = color >> 24;
		FloatingDrawable((ImDrawListPtr drawList, float percentElapsed, Vector2 pos) =>
		{
			uint num = ((percentElapsed > 0.75f) ? ((uint)((float)startingAlpha * (percentElapsed - 0.75f) * 4f) << 24) : 0u);
			pos = new Vector2(pos.X - textSize.X / 2f, pos.Y - textSize.Y - 20f * percentElapsed * ImGuiHelpers.GlobalScale);
			drawList.AddText(pos + Vector2.One * ImGuiHelpers.GlobalScale, (startingAlpha << 24) - num, text);
			drawList.AddText(pos, color - num, text);
		}, timerMS);
	}

	public static bool IsItemDragged(object id, ImGuiMouseButton button, float gridSize, bool drawGrid, out Vector2 pos)
	{
		if (!GetDragLock(id, ImGuiMouseButton.Left))
		{
			pos = Vector2.Zero;
			return false;
		}
		pos = GetMouseGridPosition(gridSize) * gridSize;
		if (drawGrid)
		{
			DrawGrid(gridSize, gridSize / 10f + 1f, Vector2.Zero);
		}
		if (initialPosition)
		{
			lastGridPosition = pos;
			initialPosition = false;
			return false;
		}
		bool flag = pos != lastGridPosition;
		if (flag)
		{
			lastGridPosition = pos;
		}
		return flag;
	}

	public static bool IsItemDraggedDelta(object id, ImGuiMouseButton button, float gridSize, bool drawGrid, out Vector2 delta)
	{
		if (!GetDragLock(id, ImGuiMouseButton.Left))
		{
			delta = Vector2.Zero;
			return false;
		}
		Vector2 offset = new Vector2(MathF.Round(gridCenter.X % gridSize), MathF.Round(gridCenter.Y % gridSize));
		Vector2 mouseGridPosition = GetMouseGridPosition(gridSize, offset);
		delta = mouseGridPosition - lastGridPosition;
		if (drawGrid)
		{
			DrawGrid(gridSize, gridSize / 10f + 1f, offset);
		}
		if (initialPosition)
		{
			lastGridPosition = mouseGridPosition;
			initialPosition = false;
			return false;
		}
		bool flag = mouseGridPosition != lastGridPosition;
		if (flag)
		{
			lastGridPosition = mouseGridPosition;
		}
		return flag;
	}

	private static bool GetDragLock(object id, ImGuiMouseButton button)
	{
		if (isDraggingItem && !ImGui.IsAnyMouseDown())
		{
			dragID = null;
			isDraggingItem = false;
		}
		if (!isDraggingItem && ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
		}
		if (1 == 0)
		{
		}
		object obj;
		if (id is string text)
		{
			obj = ImGui.GetID(text);
		}
		else
		{
			obj = ((!Util.IsNumeric(id)) ? id : ((object)ImGui.GetID(id.ToString())));
		}
		if (1 == 0)
		{
		}
		object obj2 = obj;
		if ((!isDraggingItem && !ImGui.IsItemClicked(button)) || (dragID != null && ((dragID is uint num && obj2 is uint num2) ? (num != num2) : (dragID != obj2))))
		{
			return false;
		}
		if (!isDraggingItem)
		{
			BlockWindowDrag();
			dragID = obj2;
			isDraggingItem = true;
			gridCenter = ImGui.GetMousePos();
			initialPosition = true;
		}
		if (ImGui.IsMouseDragging(button, 0f))
		{
			return true;
		}
		dragID = null;
		isDraggingItem = false;
		return false;
	}

	private static Vector2 GetMouseGridPosition(float gridSize, Vector2 offset = default(Vector2))
	{
		Vector2 vector = ImGui.GetMousePos() - offset;
		return new Vector2(MathF.Round(vector.X / gridSize), MathF.Round(vector.Y / gridSize));
	}

	private static void DrawGrid(float size, float circleRadius = 0f, Vector2 offset = default(Vector2))
	{
		ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
		Vector2 size2 = ImGuiHelpers.MainViewport.Size;
		for (float num = ((offset.X == 0f) ? size : offset.X); num < size2.X; num += size)
		{
			Vector2 p = new Vector2(num, 0f);
			Vector2 p2 = size2;
			p2.X = num;
			foregroundDrawList.AddLine(p, p2, uint.MaxValue);
		}
		for (float num2 = ((offset.Y == 0f) ? size : offset.Y); num2 < size2.Y; num2 += size)
		{
			Vector2 p3 = new Vector2(0f, num2);
			Vector2 p2 = size2;
			p2.Y = num2;
			foregroundDrawList.AddLine(p3, p2, uint.MaxValue);
		}
		if (circleRadius != 0f)
		{
			foregroundDrawList.AddCircleFilled(GetMouseGridPosition(size, offset) * size + offset, circleRadius, uint.MaxValue);
		}
	}

	[CompilerGenerated]
	internal static string _003CExcelSheetTable_003Eg__GetObjectAsString_007C29_0<T>(object o) where T : struct, IExcelRow<T>
	{
		if (1 == 0)
		{
		}
		string result = ((!(o is RowRef rowRef)) ? (o?.ToString() ?? string.Empty) : $"{rowRef.GetType().GenericTypeArguments[0].Name}#{rowRef.RowId}");
		if (1 == 0)
		{
		}
		return result;
	}

	[CompilerGenerated]
	internal static IEnumerable<string> _003CExcelSheetTable_003Eg__GetPropertiesAsStrings_007C29_1<T>(IEnumerable<PropertyInfo> properties, object o) where T : struct, IExcelRow<T>
	{
		foreach (PropertyInfo propertyInfo in properties)
		{
			yield return _003CExcelSheetTable_003Eg__GetObjectAsString_007C29_0<T>(propertyInfo.GetValue(o));
		}
	}

	[CompilerGenerated]
	internal static IComparable _003CExcelSheetTable_003Eg__GetComparable_007C29_2<T>(object o) where T : struct, IExcelRow<T>
	{
		object result;
		if (!(o is RowRef rowRef))
		{
			result = (o as IComparable) ?? _003CExcelSheetTable_003Eg__GetObjectAsString_007C29_0<T>(o);
		}
		else
		{
			IComparable comparable = rowRef.RowId;
			result = comparable;
		}
		return (IComparable)result;
	}
}
