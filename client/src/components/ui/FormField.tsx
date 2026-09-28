import type { PropsWithChildren, ReactNode } from 'react'
export function FormField({ label, htmlFor, error, children }: PropsWithChildren<{ label: string; htmlFor: string; error?: string }>) {
  return <div className="form-field"><label htmlFor={htmlFor}>{label}</label>{children}{error && <p className="form-field-error" id={`${htmlFor}-error`} role="alert">{error}</p>}</div>
}
export function Alert({ children }: { children: ReactNode }) { return <p className="ui-alert" role="alert">{children}</p> }
