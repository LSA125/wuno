// Scrabble-style letter scoring for frontend display
// Common (1pt): E,A,I,O,N,R,T,L,S,U
// Uncommon (2pt): D,G,B,C,M,P,F,H,V,W,Y,K,J
// Rare (5pt): X,Q,Z

const letterValues: Record<string, number> = {
    a: 1, e: 1, i: 1, o: 1, u: 1,
    n: 1, r: 1, t: 1, l: 1, s: 1,
    d: 2, g: 2, b: 2, c: 2, m: 2,
    p: 2, f: 2, h: 2, v: 2, w: 2,
    y: 2, k: 2, j: 2,
    x: 5, q: 5, z: 5
};

export function getLetterValue(char: string): number {
    return letterValues[char.toLowerCase()] ?? 1;
}

export function getLetterScore(char: string, multiplier: number): number {
    return getLetterValue(char) * multiplier;
}
