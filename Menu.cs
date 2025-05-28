using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class Button {

    private Media media;
    private string text;
    private Action callback;
    private bool isDown;
    private int animationCounter;

    public Button(
        Media media, 
        string text,
        Action callback) {
        this.media = media;
        this.text = text;
        this.callback = callback;
        isDown = false;
    }

    public void Draw(
        IntPtr window, 
        IntPtr renderer,
        int x,
        int y) {
    
        media.Rectangle(renderer, x, y, 13, 14, Colour.White);

        media.DrawText(text, renderer, x, y, Colour.Black);
    }

    public void Click() {
        isDown = true;
        animationCounter = 0;
    }

    public void Tick() {
        animationCounter++;
        if (animationCounter == 30)
            animationCounter = 0;
    }
}



public class Menu {

    private Media media;

    public Menu(Media media) {
        this.media = media;
    }

    public void Draw(IntPtr window, IntPtr renderer) {

        

         // Draw buttons
 
    }
      

}
