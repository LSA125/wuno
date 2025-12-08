export const normalizeWord = (word: string) =>
    word
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .replace(/[^a-z]/g, "");

export const reverseString = (str: string) => str.split("").reverse().join("");

export const computeReverseMatchLength = (typed: string, reversed: string) => {
    let len = 0;
    while (len < typed.length && len < reversed.length && typed[len] === reversed[len]) len++;
    return len;
};