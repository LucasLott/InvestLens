export interface ConfiguracaoRequest { plMaximo: number; dyMinimo: number; roeMinimo: number; dividaPatrimonioMaximo: number; margemLiquidaMinimo: number; retornoPreco: number }
export interface ConfiguracaoResponse extends ConfiguracaoRequest { idConfiguracao: number; idUsuario: number; dataInclusao: string; dataAlteracao: string | null }
