export default function EffectChip({ label }: { label: string }) {
    return (
        <span
            className="badge text-bg-info animate-in fade-in zoom-in duration-300"
            style={{ animation: "toastIn 180ms ease-out" }}
        >
            {label}
        </span>
    );
}
