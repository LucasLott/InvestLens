export function sanitizeCpf(value: string) {
  return value.replace(/\D/g, '').slice(0, 11)
}

export function formatCpf(value: string) {
  const cpf = sanitizeCpf(value)
  if (cpf.length <= 3) return cpf
  if (cpf.length <= 6) return `${cpf.slice(0, 3)}.${cpf.slice(3)}`
  if (cpf.length <= 9) return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6)}`
  return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`
}

export function isValidCpf(value: string) {
  const cpf = sanitizeCpf(value)
  if (cpf.length !== 11 || /^([0-9])\1{10}$/.test(cpf)) return false

  const calculateDigit = (length: number, initialWeight: number) => {
    const sum = Array.from({ length }, (_, index) => (cpf.charCodeAt(index) - 48) * (initialWeight - index))
      .reduce((total, current) => total + current, 0)
    const remainder = sum % 11
    return remainder < 2 ? 0 : 11 - remainder
  }

  return calculateDigit(9, 10) === cpf.charCodeAt(9) - 48
    && calculateDigit(10, 11) === cpf.charCodeAt(10) - 48
}
