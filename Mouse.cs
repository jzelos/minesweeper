using static SDL2.SDL;

namespace Zelos.Minesweeper;

class Mouse
{

    public const byte MaxButtons = 8;

    public nint X {get; private set;}
    public nint Y {get; private set;}

    public byte[] ButtonState {get; private set;} = new byte[MaxButtons]; 

/*     public void UpdateCordinates() {
         SDL_GetMouseState(X, Y);
    }
 */
    public void ProcessButtonEvent(SDL_MouseButtonEvent e) {

        if (e.button < MaxButtons) {
            ButtonState[e.button] = e.state;
            X = e.x;
            Y = e.y;
        }
    }
}