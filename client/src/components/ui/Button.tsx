import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'ghost'; loading?: boolean }
export function Button({ variant = 'primary', loading = false, children, className = '', disabled, ...props }: PropsWithChildren<ButtonProps>) {
  return <button className={`ui-button ui-button-${variant} ${className}`} disabled={disabled || loading} aria-busy={loading} {...props}>{children}</button>
}
