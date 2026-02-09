using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class MenuItem {
    public string Text { get; set; }
    public Action? Action { get; set; }
    public List<MenuItem> SubItems { get; set; }
    public bool IsSeparator { get; set; }

    public MenuItem(string text, Action? action = null) {
        Text = text;
        Action = action;
        SubItems = new List<MenuItem>();
        IsSeparator = false;
    }

    public static MenuItem Separator() {
        return new MenuItem("") { IsSeparator = true };
    }
}

public class MenuBar {
    private Media media;
    private List<MenuItem> items;
    private int? hoveredIndex;
    private int? openMenuIndex;
    private int? hoveredSubItemIndex;
    
    private const int MENU_HEIGHT = 30;
    private const int MENU_ITEM_PADDING = 10;
    private const int SUBMENU_WIDTH = 150;
    private const int SUBMENU_ITEM_HEIGHT = 20;
    private const int SEPARATOR_HEIGHT = 5;

    public MenuBar(Media media) {
        this.media = media;
        this.items = new List<MenuItem>();
        this.hoveredIndex = null;
        this.openMenuIndex = null;
        this.hoveredSubItemIndex = null;
    }

    public void AddMenu(MenuItem item) {
        items.Add(item);
    }

    public void DrawBar(IntPtr window, IntPtr renderer, int windowWidth) {
        // Draw menu bar background
        media.Rectangle(renderer, 0, 0, windowWidth, MENU_HEIGHT, Colour.MenuBar);

        int x = 5;
        for (int i = 0; i < items.Count; i++) {
            var item = items[i];
            int textWidth = MeasureText(item.Text);
            int itemWidth = textWidth + MENU_ITEM_PADDING * 2;

            // Highlight if hovered or open
            if (i == hoveredIndex || i == openMenuIndex) {
                media.Rectangle(renderer, x, 0, itemWidth, MENU_HEIGHT, Colour.MenuHighlight);
            }

            // Draw text - adjusted vertical position for better centering
            media.DrawText(item.Text, renderer, x + MENU_ITEM_PADDING, 8, Colour.Black);

            x += itemWidth;
        }

        // Draw bottom border
        media.Rectangle(renderer, 0, MENU_HEIGHT - 1, windowWidth, 1, Colour.Gray);
    }

    public void DrawSubmenus(IntPtr renderer) {
        if (openMenuIndex.HasValue) {
            var item = items[openMenuIndex.Value];
            if (item.SubItems.Count > 0) {
                int x = GetMenuXPosition(openMenuIndex.Value);
                DrawSubmenu(renderer, item, x, MENU_HEIGHT);
            }
        }
    }

    private void DrawSubmenu(IntPtr renderer, MenuItem parentItem, int x, int y) {
        int height = CalculateSubmenuHeight(parentItem);
        
        // Draw drop shadow
        media.Rectangle(renderer, x + 3, y + 3, SUBMENU_WIDTH, height, Colour.MenuShadow);
        
        // Draw submenu background
        media.Rectangle(renderer, x, y, SUBMENU_WIDTH, height, Colour.MenuBar);
        
        // Draw border
        DrawBorder(renderer, x, y, SUBMENU_WIDTH, height);

        int itemY = y;
        for (int i = 0; i < parentItem.SubItems.Count; i++) {
            var subItem = parentItem.SubItems[i];

            if (subItem.IsSeparator) {
                // Draw separator line
                itemY += 2;
                media.Rectangle(renderer, x + 5, itemY, SUBMENU_WIDTH - 10, 1, Colour.Gray);
                itemY += 3;
            } else {
                // Highlight if hovered
                if (i == hoveredSubItemIndex) {
                    media.Rectangle(renderer, x + 1, itemY + 1, SUBMENU_WIDTH - 2, SUBMENU_ITEM_HEIGHT - 2, Colour.MenuHighlight);
                }

                // Draw text - adjusted vertical position
                media.DrawText(subItem.Text, renderer, x + 10, itemY + 3, Colour.Black);
                itemY += SUBMENU_ITEM_HEIGHT;
            }
        }
    }

    private void DrawBorder(IntPtr renderer, int x, int y, int width, int height) {
        // Top
        media.Rectangle(renderer, x, y, width, 1, Colour.Gray);
        // Bottom
        media.Rectangle(renderer, x, y + height - 1, width, 1, Colour.Gray);
        // Left
        media.Rectangle(renderer, x, y, 1, height, Colour.Gray);
        // Right
        media.Rectangle(renderer, x + width - 1, y, 1, height, Colour.Gray);
    }

    private int CalculateSubmenuHeight(MenuItem item) {
        int height = 0;
        foreach (var subItem in item.SubItems) {
            if (subItem.IsSeparator) {
                height += SEPARATOR_HEIGHT;
            } else {
                height += SUBMENU_ITEM_HEIGHT;
            }
        }
        return height;
    }

    private int MeasureText(string text) {
        // Approximate text width (can be improved with actual measurement)
        return text.Length * 10;
    }

    public void HandleMouseMove(int mouseX, int mouseY) {
        if (mouseY < MENU_HEIGHT) {
            // Mouse is in the menu bar
            hoveredIndex = GetMenuIndexAtPosition(mouseX);
            
            // If a menu is open and we hover over another menu, open that one instead
            if (openMenuIndex.HasValue && hoveredIndex.HasValue && hoveredIndex != openMenuIndex) {
                openMenuIndex = hoveredIndex;
                hoveredSubItemIndex = null;
            }
        } else if (openMenuIndex.HasValue) {
            // Check if mouse is over submenu
            var item = items[openMenuIndex.Value];
            int menuX = GetMenuXPosition(openMenuIndex.Value);
            
            if (mouseX >= menuX && mouseX < menuX + SUBMENU_WIDTH) {
                int relativeY = mouseY - MENU_HEIGHT;
                hoveredSubItemIndex = GetSubmenuIndexAtPosition(item, relativeY);
            } else {
                hoveredSubItemIndex = null;
            }
        }
    }

    public bool HandleMouseClick(int mouseX, int mouseY) {
        if (mouseY < MENU_HEIGHT) {
            // Click on menu bar
            int? clickedIndex = GetMenuIndexAtPosition(mouseX);
            if (clickedIndex.HasValue) {
                if (openMenuIndex == clickedIndex) {
                    // Close if clicking the same menu
                    openMenuIndex = null;
                    hoveredSubItemIndex = null;
                } else {
                    // Open clicked menu
                    openMenuIndex = clickedIndex;
                    hoveredSubItemIndex = null;
                }
                return true;
            }
        } else if (openMenuIndex.HasValue) {
            // Click on submenu
            var item = items[openMenuIndex.Value];
            int menuX = GetMenuXPosition(openMenuIndex.Value);
            
            if (mouseX >= menuX && mouseX < menuX + SUBMENU_WIDTH) {
                int relativeY = mouseY - MENU_HEIGHT;
                int? clickedSubIndex = GetSubmenuIndexAtPosition(item, relativeY);
                
                if (clickedSubIndex.HasValue) {
                    var subItem = item.SubItems[clickedSubIndex.Value];
                    if (!subItem.IsSeparator && subItem.Action != null) {
                        subItem.Action.Invoke();
                        openMenuIndex = null;
                        hoveredSubItemIndex = null;
                        return true;
                    }
                }
            } else {
                // Click outside submenu - close it
                openMenuIndex = null;
                hoveredSubItemIndex = null;
                return true;
            }
        } else if (openMenuIndex.HasValue) {
            // Click outside menu - close it
            openMenuIndex = null;
            hoveredSubItemIndex = null;
        }
        
        return false;
    }

    private int? GetMenuIndexAtPosition(int mouseX) {
        int x = 5;
        for (int i = 0; i < items.Count; i++) {
            var item = items[i];
            int textWidth = MeasureText(item.Text);
            int itemWidth = textWidth + MENU_ITEM_PADDING * 2;
            
            if (mouseX >= x && mouseX < x + itemWidth) {
                return i;
            }
            
            x += itemWidth;
        }
        return null;
    }

    private int GetMenuXPosition(int menuIndex) {
        int x = 5;
        for (int i = 0; i < menuIndex; i++) {
            var item = items[i];
            int textWidth = MeasureText(item.Text);
            int itemWidth = textWidth + MENU_ITEM_PADDING * 2;
            x += itemWidth;
        }
        return x;
    }

    private int? GetSubmenuIndexAtPosition(MenuItem parentItem, int relativeY) {
        if (relativeY < 0) return null;
        
        int itemY = 0;
        for (int i = 0; i < parentItem.SubItems.Count; i++) {
            var subItem = parentItem.SubItems[i];
            
            if (subItem.IsSeparator) {
                itemY += SEPARATOR_HEIGHT;
            } else {
                if (relativeY >= itemY && relativeY < itemY + SUBMENU_ITEM_HEIGHT) {
                    return i;
                }
                itemY += SUBMENU_ITEM_HEIGHT;
            }
        }
        return null;
    }

    public int GetHeight() {
        return MENU_HEIGHT;
    }

    public bool IsMenuOpen() {
        return openMenuIndex.HasValue;
    }

    public void CloseMenus() {
        openMenuIndex = null;
        hoveredSubItemIndex = null;
        hoveredIndex = null;
    }
}
