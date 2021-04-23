using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils{
    public static Vector2 getMousePosition(){
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public static bool isBetween(Vector2 target, Vector2 one, Vector2 two){
        //return Vector2.Dot((one.normalized + two.normalized) / 2, target) < 0;
        return !(Vector3.Cross(one, target).z * Vector3.Cross(one, two).z >= 0 && Vector3.Cross(two, target).z * Vector3.Cross(two, one).z >= 0);
    }

    public static bool isInVision(int x1, int y1, int x2, int y2, float[,] cells){
        if(x2 == x1)
            return isInVisionVertical(x1, y1, y2, cells);
        
        float smallerX = x1 + 0.5f;
        float smallerY = y1 + 0.5f;

        float largerX = x2 + 0.5f;
        float largerY = y2 + 0.5f;

        if(smallerX > largerX){
            smallerX = x2 + 0.5f;
            smallerY = y2 + 0.5f;

            largerX = x1 + 0.5f;
            largerY = y1 + 0.5f;
        }

        float slope = (largerY - smallerY) / (largerX - smallerX);

        //bool debug = x1 == 6 && y1 == 6 && x2 == 7 && y2 == 3;
        //if(debug){
        //    Debug.Log(slope + " " + smallerY + " " + smallerX + " " + largerY + " " + largerX + " " + (1 / slope));
        //    Debug.Break();
        //}

        if(Mathf.Abs(slope) <= 1)
            return isInVisionXIterate(smallerX, smallerY, largerX, largerY, cells, slope, false);
        else
            return isInVisionXIterate(smallerY, smallerX, largerY, largerX, cells, 1 / slope, true);
    }

    private static bool isInVisionXIterate(float smallerX, float smallerY, float largerX, float largerY, float[,] cells, float slope, bool reverse){
        if(smallerX > largerX)
            for(int i = Mathf.CeilToInt(largerX); i <= Mathf.FloorToInt(smallerX); i++){
                int y = (int) ((i - largerX) * slope + largerY);

                if(reverse){
                    if(cells[y, i - 1] == float.MinValue || cells[y, i] == float.MinValue)
                        return false;
                }else{
                    if(cells[i - 1, y] == float.MinValue || cells[i, y] == float.MinValue)
                        return false;
                }
            }
        else
            for(int i = Mathf.CeilToInt(smallerX); i <= Mathf.FloorToInt(largerX); i++){
                int y = (int) ((i - smallerX) * slope + smallerY);

                if(reverse){
                    if(cells[y, i - 1] == float.MinValue || cells[y, i] == float.MinValue)
                        return false;
                }else{
                    if(cells[i - 1, y] == float.MinValue || cells[i, y] == float.MinValue)
                        return false;
                }
            }

        return true;
    }

    private static bool isInVisionVertical(int x, int y1, int y2, float[,] cells){
        int smaller = y1;
        int larger = y2;

        if(smaller > larger){
            smaller = y2;
            larger = y1;
        }

        for(int i = smaller; i <= larger; i++)
            if(cells[x, i] == float.MinValue)
                return false;

        return true;
    }
}
