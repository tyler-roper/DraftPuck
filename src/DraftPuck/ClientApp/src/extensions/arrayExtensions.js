// Returns number between 0 (inclusive) and 1.0 (exclusive),
// just like Math.random().
function random(i) {
    let mw = 123456789;
    let mz = 987654321;
    const mask = 0xffffffff;
    mw = (123456789 + i) & mask;
    mz = (987654321 - i) & mask;
    mz = (36969 * (mz & 65535) + (mz >> 16)) & mask;
    mw = (18000 * (mw & 65535) + (mw >> 16)) & mask;
    let result = ((mz << 16) + (mw & 65535)) >>> 0;
    result /= 4294967296;
    return result;
}
if (!Array.prototype.random) {
    Array.prototype.random = function () {
        return this[Math.floor(Math.random() * this.length)];
    };
}
if (!Array.prototype.seed) {
    Array.prototype.seed = function (seed) {
        return this[Math.floor(random(seed) * this.length)];
    };
}
export {};
//# sourceMappingURL=arrayExtensions.js.map