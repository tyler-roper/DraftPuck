interface PatchOperation {
    op: "add" | "remove" | "replace" | "copy" | "move" | "test";
    path: string;
    value: object;
}