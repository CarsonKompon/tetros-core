using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Tetros;


public enum BlockType { Empty, I, O, T, S, Z, J, L };
public class TetrosShape
{
    public int[] Blocks {get; set;}

    public int[] GetGrid()
    {
        var grid = new int[16];
        for(int i=0; i<16; i++)
        {
            var x = i % 4;
            var y = i / 4;
            if(Blocks.Contains(i))
            {
                grid[i] = 1;
            }
            else
            {
                grid[i] = 0;
            }
        }
        return grid;
    }
}

public static class TetrosShapes
{
    public static readonly TetrosShape[] I = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,6,7}},
        new TetrosShape {Blocks = new int[4] {2,6,10,14}},
        new TetrosShape {Blocks = new int[4] {8,9,10,11}},
        new TetrosShape {Blocks = new int[4] {1,5,9,13}}
    };

    public static readonly TetrosShape[] O = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}}
    };

    public static readonly TetrosShape[] T = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {1,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,6,9}},
        new TetrosShape {Blocks = new int[4] {4,5,6,9}},
        new TetrosShape {Blocks = new int[4] {1,4,5,9}}
    };

    public static readonly TetrosShape[] S = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {1,2,4,5}},
        new TetrosShape {Blocks = new int[4] {0,4,5,9}},
        new TetrosShape {Blocks = new int[4] {1,2,4,5}},
        new TetrosShape {Blocks = new int[4] {0,4,5,9}}
    };

    public static readonly TetrosShape[] Z = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {0,1,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,4,8}},
        new TetrosShape {Blocks = new int[4] {0,1,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,4,8}}
    };

    public static readonly TetrosShape[] J = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {0,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,2,5,9}},
        new TetrosShape {Blocks = new int[4] {4,5,6,10}},
        new TetrosShape {Blocks = new int[4] {1,5,9,8}}
    };

    public static readonly TetrosShape[] L = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {2,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,9,10}},
        new TetrosShape {Blocks = new int[4] {4,5,6,8}},
        new TetrosShape {Blocks = new int[4] {0,1,5,9}}
    };

    public static TetrosShape GetShape(BlockType blockType, int rotation)
    {
        if(rotation < 0) rotation += 4;
        switch(blockType)
        {
            case BlockType.I:
                return I[rotation];
            case BlockType.O:
                return O[rotation];
            case BlockType.T:
                return T[rotation];
            case BlockType.S:
                return S[rotation];
            case BlockType.Z:
                return Z[rotation];
            case BlockType.J:
                return J[rotation];
            case BlockType.L:
                return L[rotation];
            default:
                return I[rotation];
        }
    }
}