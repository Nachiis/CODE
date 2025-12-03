/*
* 给定一个正则表达式，构建对应的NFA（非确定有限自动机）。
* 使用Thompson构造法将正则表达式转换为NFA。
* 示例:
* 输入:(a|b)*b
* 输出:
* START:1
* 1->2:epsilon
* 2->3:a
* ...
* ACCEPT:12
*/

#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <stack>
#include <set>
#include <map>
#include <queue>
#include <cctype>

struct NFANode;

// -------------------- NFA 相关结构 --------------------

struct NFAEdge {
    char symbol;     // '\0' 表示 epsilon
    NFANode* to;
};

struct NFANode {
    int id; // 状态标识符 1,2,3,...
    std::vector<NFAEdge> edges;

    NFANode(int _id) : id(_id) {}
};

struct NFA {
    NFANode* start = nullptr;
    NFANode* accept = nullptr;
    std::vector<NFANode*> states;
    std::set<char> alphabet;   // 不包含 epsilon
	std::string regex;        // 原始正则表达式
};

// Thompson 构造中的碎片
struct NFAFragment {
    NFANode* start;
    NFANode* accept;
};

// -------------------- 工具函数：判断字符类型 --------------------

// '.' 作为显式连接运算符
bool isOperator(char c) {
    return c == '|' || c == '*' || c == '(' || c == ')' || c == '.';
}

bool isLiteral(char c) {
    return !isOperator(c) && !isspace(static_cast<unsigned char>(c));
}

// -------------------- 第一步：在正则式中显式插入连接符 '.' --------------------
// 例如： (a|b)*b  ->  (a|b)*.b
std::string insertConcatOperators(const std::string& regex) {
    std::string result;
    char prev = 0;

    for (size_t i = 0; i < regex.size(); ++i) {
        char c = regex[i];
        if (isspace(static_cast<unsigned char>(c))) {
            continue; // 忽略空白
        }

        if (!result.empty()) {
            bool prevIsLiteralOrRightOrStar =
                isLiteral(prev) || prev == ')' || prev == '*';
            bool currIsLiteralOrLeft =
                isLiteral(c) || c == '(';

            if (prevIsLiteralOrRightOrStar && currIsLiteralOrLeft) {
                result.push_back('.'); // 显式连接符
            }
        }

        result.push_back(c);
        prev = c;
    }

    return result;
}

// -------------------- 第二步：Shunting-yard 转后缀表达式 --------------------
// 优先级： '*' > '.' > '|'
int precedence(char op) {
    switch (op) {
    case '*': return 3;
    case '.': return 2;
    case '|': return 1;
    default:  return 0;
    }
}

std::string toPostfix(const std::string& regexWithConcat) {
    std::string output;
    std::stack<char> opStack; // 运算符栈

    for (char c : regexWithConcat) {
        if (isLiteral(c)) {
            output.push_back(c);
        }
        else if (c == '(') {
            opStack.push(c);
        }
        else if (c == ')') {
            while (!opStack.empty() && opStack.top() != '(') {
                output.push_back(opStack.top());
                opStack.pop();
            }
            if (!opStack.empty() && opStack.top() == '(') {
                opStack.pop();
            }
            else {
                std::cerr << "Error: mismatched parentheses in regex.\n";
            }
        }
        else if (c == '*') {
            // '*' 是后缀一元运算，直接输出
            output.push_back(c);
        }
        else if (c == '|' || c == '.') {
            while (!opStack.empty() && opStack.top() != '(' &&
                precedence(opStack.top()) >= precedence(c)) {
                output.push_back(opStack.top());
                opStack.pop();
            }
            opStack.push(c);
        }
        else {
            std::cerr << "Warning: unknown character in regex: " << c << "\n";
        }
    }

    while (!opStack.empty()) {
        if (opStack.top() == '(' || opStack.top() == ')') {
            std::cerr << "Error: mismatched parentheses in regex.\n";
        }
        output.push_back(opStack.top());
        opStack.pop();
    }

    return output;
}

// -------------------- 第三步：Thompson 构造 NFA --------------------

class NFAFactory {
public:
    NFAFactory() : nextId(1) {}

    NFA buildFromRegex(const std::string& regex) {
        std::string withConcat = insertConcatOperators(regex);
        std::string postfix = toPostfix(withConcat);
        // std::cerr << "postfix: " << postfix << "\n";

        std::stack<NFAFragment> st;

        for (char c : postfix) {
            if (isLiteral(c)) {
                st.push(buildLiteral(c));
            }
            else if (c == '.') {
                if (st.size() < 2) {
                    std::cerr << "Error: invalid regex (concat stack underflow).\n";
                    break;
                }
                NFAFragment right = st.top(); st.pop();
                NFAFragment left = st.top(); st.pop();
                st.push(buildConcat(left, right));
            }
            else if (c == '|') {
                if (st.size() < 2) {
                    std::cerr << "Error: invalid regex (union stack underflow).\n";
                    break;
                }
                NFAFragment right = st.top(); st.pop();
                NFAFragment left = st.top(); st.pop();
                st.push(buildUnion(left, right));
            }
            else if (c == '*') {
                if (st.empty()) {
                    std::cerr << "Error: invalid regex (star stack underflow).\n";
                    break;
                }
                NFAFragment frag = st.top(); st.pop();
                st.push(buildStar(frag));
            }
            else {
				std::cerr << "Warning: unknown character in postfix regex: " << c << "\n";
            }
        }

        if (st.size() != 1) {
            std::cerr << "Error: invalid regex, stack size: " << st.size() << "\n";
        }

        NFA nfa;
        if (!st.empty()) {
            NFAFragment finalFrag = st.top();
            nfa.start = finalFrag.start;
            nfa.accept = finalFrag.accept;
        }
        nfa.states = allNodes;
        nfa.alphabet = alphabet;
		nfa.regex = regex;
        return nfa;
    }

private:
    int nextId;
    std::vector<NFANode*> allNodes;
    std::set<char> alphabet;

    NFANode* newNode() {
        NFANode* node = new NFANode(nextId++);
        allNodes.push_back(node);
        return node;
    }

    NFAFragment buildLiteral(char c) {
        NFANode* s = newNode();
        NFANode* t = newNode();
        s->edges.push_back({ c, t });
        alphabet.insert(c);
        return { s, t };
    }

    NFAFragment buildConcat(const NFAFragment& left, const NFAFragment& right) {
        left.accept->edges.push_back({ '\0', right.start });
        return { left.start, right.accept };
    }

    NFAFragment buildUnion(const NFAFragment& left, const NFAFragment& right) {
        NFANode* s = newNode();
        NFANode* t = newNode();
        s->edges.push_back({ '\0', left.start });
        s->edges.push_back({ '\0', right.start });
        left.accept->edges.push_back({ '\0', t });
        right.accept->edges.push_back({ '\0', t });
        return { s, t };
    }

    NFAFragment buildStar(const NFAFragment& frag) {
        NFANode* s = newNode();
        NFANode* t = newNode();
        s->edges.push_back({ '\0', frag.start });
        s->edges.push_back({ '\0', t });
        frag.accept->edges.push_back({ '\0', frag.start });
        frag.accept->edges.push_back({ '\0', t });
        return { s, t };
    }
};

// -------------------- 输出 NFA --------------------

void printNFA(const NFA& nfa, std::ostream& out) {
    out << "--------------------------------------------------\n";
    if (!nfa.start || !nfa.accept) {
        out << "Empty NFA.\n";
        return;
    }
	out << "# NFA for regex: " << nfa.regex << "\n";
    out << "START:" << nfa.start->id << "\n";

    for (auto node : nfa.states) {
        for (const auto& e : node->edges) {
            out << node->id << "->" << e.to->id << ":";
            if (e.symbol == '\0') {
                out << "epsilon";
            }
            else {
                out << e.symbol;
            }
            out << "\n";
        }
    }

    out << "ACCEPT:" << nfa.accept->id << "\n";

    out << "# Alphabet: ";
    bool first = true;
    for (char c : nfa.alphabet) {
        if (!first) out << ", ";
        out << c;
        first = false;
    }
    out << "\n";
}

// -------------------- NFA -> DFA (子集构造) --------------------

struct DFAState {
    int id = 0;                       // 0..n-1
    bool isAccept = false;
    std::map<char, int> trans;    // symbol -> state id
    // 从构造算法中可知，一个 DFA 状态对应多个 NFA 状态
	// 用 set 存储这些 NFA 状态的索引
    std::set<int> nfaStates;      // 这个 DFA 状态对应的 NFA 状态集合（索引）
};

struct DFA {
    int start = 0;
    std::vector<DFAState> states;
    std::set<char> alphabet;
};

 //@brief 计算 NFA 状态集合 S 的 epsilon 闭包
 //@param S NFA 状态集合（索引）
 //@param nfaStates NFA 状态数组，包含NFA的所有状态节点
 //@param nodeIndex NFA 状态到索引的映射
 //@return epsilon 闭包
std::set<int> epsilonClosure(const std::set<int>& S,
    const std::vector<NFANode*>& nfaStates,
    const std::map<NFANode*, int>& nodeIndex) {

    std::set<int> closure = S;
    std::stack<int> st;
    for (int s : S) st.push(s);

    while (!st.empty()) {
        int i = st.top();
        st.pop();
        NFANode* node = nfaStates[i];
        for (const auto& e : node->edges) {
			if (e.symbol == '\0') { 
                // 空转移，则加入到闭包中
                int j = nodeIndex.at(e.to);
				if (closure.insert(j).second) { 
                    // 能加入到闭包，说明是新状态，需要再次求这个新状态的 epsilon 闭包
                    st.push(j);
                }
            }
        }
    }
    return closure;
}


/// @brief 计算 NFA 状态集合 S 在符号 symbol 下的转移结果
/// @param S NFA 状态集合（索引）
/// @param symbol 符号
/// @param nfaStates NFA 状态数组，包含NFA的所有状态节点
/// @param nodeIndex NFA 状态到索引的映射
/// @return 转移结果
std::set<int> moveOnSymbol(const std::set<int>& S, char symbol,
    const std::vector<NFANode*>& nfaStates,
    const std::map<NFANode*, int>& nodeIndex) {

    std::set<int> result;
    for (int i : S) {
        NFANode* node = nfaStates[i];
        for (const auto& e : node->edges) {
            if (e.symbol == symbol) {
                int j = nodeIndex.at(e.to);
                result.insert(j);
            }
        }
    }
    return result;
}

DFA nfaToDfa(const NFA& nfa) {
    DFA dfa;
    dfa.alphabet = nfa.alphabet;

    if (!nfa.start || !nfa.accept) {
        return dfa;
    }

    // 建立 NFA 节点到索引的映射
    std::map<NFANode*, int> nodeIndex;
    for (size_t i = 0; i < nfa.states.size(); ++i) {
        nodeIndex[nfa.states[i]] = (int)i;
    }
	// 起始和接受状态索引
    int startIdx = nodeIndex[nfa.start];
    int acceptIdx = nodeIndex[nfa.accept];

    // 初始子集：epsilon-closure({start})
    std::set<int> startSet = { startIdx };
    startSet = epsilonClosure(startSet, nfa.states, nodeIndex);

    std::map<std::set<int>, int> subsetToId;
	std::queue<int> q;// q 存储 DFA 状态 id

    DFAState startState;
	// 初始化第一个 DFA 状态
    startState.id = 0;
    startState.nfaStates = startSet;
    startState.isAccept = (startSet.count(acceptIdx) > 0);
    dfa.states.push_back(startState);
    subsetToId[startSet] = 0;
    q.push(0);

    // 子集构造 BFS
    while (!q.empty()) {
        int sid = q.front(); q.pop();
		// 遍历所有字母，计算转移 move({states,...},c)
        for (char c : dfa.alphabet) {
            std::set<int> moveSet = moveOnSymbol(dfa.states[sid].nfaStates, c, nfa.states, nodeIndex);
            if (moveSet.empty()) continue;
			// 计算 move({states,...},c) 的 epsilon-closure
            std::set<int> targetSet = epsilonClosure(moveSet, nfa.states, nodeIndex);
            if (targetSet.empty()) continue;

            int tid;
            auto it = subsetToId.find(targetSet);
            if (it == subsetToId.end()) {
				// 没找到，说明是新状态
                tid = (int)dfa.states.size();
                DFAState ns;
                ns.id = tid;
                ns.nfaStates = targetSet;
                ns.isAccept = (targetSet.count(acceptIdx) > 0);
				dfa.states.push_back(ns);
                subsetToId[targetSet] = tid;
                q.push(tid);
            }
            else {
                tid = it->second;
            }
            // states 其实就是转移表的第一列
            // trans 就是对应转移表 n 个字符的列
            dfa.states[sid].trans[c] = tid;
        }
    }

    dfa.start = 0;
    return dfa;
}

// -------------------- DFA 最小化（表填充法） --------------------

struct MinDFAState {
    int id = 0;
    bool isAccept = false;
    std::map<char, int> trans;
};

struct MinDFA {
    int start = 0;
    std::vector<MinDFAState> states;
    std::set<char> alphabet;
};

// 并查集
struct DSU {
    std::vector<int> parent;
    DSU(int n = 0) { reset(n); }
    void reset(int n) {
        parent.assign(n, 0);
        for (int i = 0; i < n; ++i) parent[i] = i;
    }
    int find(int x) {
        if (parent[x] != x) parent[x] = find(parent[x]);
        return parent[x];
    }
    void unite(int a, int b) {
        a = find(a); b = find(b);
        if (a != b) parent[b] = a;
    }
};

MinDFA minimizeDFA(const DFA& dfa) {
    MinDFA mdfa;
    mdfa.alphabet = dfa.alphabet;

    int N = (int)dfa.states.size();
    if (N == 0) return mdfa;

    // 1. 先只保留从 start 开始，整个 DFA 可达的状态
    std::vector<bool> vis(N, false);
    std::queue<int> q;
    q.push(dfa.start);
    vis[dfa.start] = true;
    while (!q.empty()) {
        int s = q.front(); q.pop();
        for (const auto& kv : dfa.states[s].trans) {
			// 直接取转移目标状态
            int t = kv.second;
            if (!vis[t]) {
                vis[t] = true;
                q.push(t);
            }
        }
    }

	// 把 Start 可达的状态重新编号，这样就滤过了一些不可达状态
    std::vector<int> old2reach(N, -1);
    std::vector<int> reach2old;
    for (int i = 0; i < N; ++i) {
        if (vis[i]) {
            old2reach[i] = (int)reach2old.size();
            reach2old.push_back(i);
        }
    }

	// R 是过滤后的状态数
    int R = (int)reach2old.size();
    if (R == 0) return mdfa;

    // 2. 检查是否需要显式的“死状态”（sink）
    bool needSink = false;
    for (int r = 0; r < R; ++r) {
		int old = reach2old[r]; // 找到原 DFA 中对应的状态
        for (char c : mdfa.alphabet) {
            auto it = dfa.states[old].trans.find(c);
            if (it == dfa.states[old].trans.end()) {
				// 说明存在一个状态，在某个字母下没有转移
                needSink = true;
                break;
            }
        }
        if (needSink) break;
    }

    int sinkIndex = -1;
    int M = R + (needSink ? 1 : 0); // 总状态数（含 sink）
    if (needSink) sinkIndex = R;

    std::vector<bool> isAccept(M, false);
    std::vector<std::map<char, int>> trans(M);

    // 3. 填写 R 个可达状态的转移（缺的指向 sink）
    for (int r = 0; r < R; ++r) {
        int old = reach2old[r];
        isAccept[r] = dfa.states[old].isAccept;
        for (char c : mdfa.alphabet) {
            auto it = dfa.states[old].trans.find(c);
            int to;
            if (it == dfa.states[old].trans.end()) {
                if (needSink) {
                    to = sinkIndex;
                }
                else {
                    // 不需要 sink：就保持部分函数
                    continue;
                }
            }
            else {
                int oldTo = it->second;
                if (old2reach[oldTo] == -1) {
                    if (needSink) {
                        to = sinkIndex;
                    }
                    else {
                        continue;
                    }
                }
                else {
                    to = old2reach[oldTo];
                }
            }
            trans[r][c] = to;
        }
    }

    // 4. sink 状态（如果需要）：非接受，自环
    if (needSink) {
        isAccept[sinkIndex] = false;
        for (char c : mdfa.alphabet) {
            trans[sinkIndex][c] = sinkIndex;
        }
    }

    // 5. 表填充法标记“可区分”的状态对
    // 二维表，M 为总状态数，上三角有效
    std::vector<std::vector<bool>> diff(M, std::vector<bool>(M, false));

    // 5.1 初始：接受 / 非接受
    for (int i = 0; i < M; ++i) {
        for (int j = i + 1; j < M; ++j) {
            if (isAccept[i] != isAccept[j]) {
                // 也就是最开始分成的两个集合
                diff[i][j] = true;
            }
        }
    }

    // 5.2 迭代细化
    bool changed = true;
    while (changed) {
        changed = false;
        for (int i = 0; i < M; ++i) {
            for (int j = i + 1; j < M; ++j) {
                if (diff[i][j]) 
                    continue;
				// 说明 i,j 目前不可区分
                // 对所有字母检查 δ(i,a)、δ(j,a) 是否已经区分
                bool mark = false;
                // 遍历所有文字
                for (char c : mdfa.alphabet) {
                    auto it1 = trans[i].find(c);
                    auto it2 = trans[j].find(c);
                    int p = (it1 == trans[i].end()) ? -1 : it1->second;
                    int q = (it2 == trans[j].end()) ? -1 : it2->second;
                    if (p == -1 || q == -1) {
                        // 正常情况：如果有 sink，则不会出现 -1
                        if (p != q) {
                            mark = true; // 一个有边，一个没边 -> 可区分
                            break;
                        }
                        else {
                            continue;
                        }
                    }
                    if (p == q) 
						continue; // 转移到同一状态，继续检查下一个字母
                    int a = std::min(p, q);
					int b = std::max(p, q); // 确保访问上三角
                    if (diff[a][b]) {
                        mark = true;
                        break;
                    }
                }
                if (mark) {
                    diff[i][j] = true;
                    changed = true;
                }
            }
        }
    }

    // 6. 用并查集合并“不可区分”的状态
    DSU dsu(M);
    for (int i = 0; i < M; ++i) {
        for (int j = i + 1; j < M; ++j) {
            if (!diff[i][j]) {
                dsu.unite(i, j);
            }
        }
    }

    // 7. 给每个等价类分配新的编号
    std::map<int, int> rootToNew;
    std::vector<int> classOf(M, -1);
    int newCount = 0;
    for (int i = 0; i < M; ++i) {
        int r = dsu.find(i);
        auto it = rootToNew.find(r);
        if (it == rootToNew.end()) {
            rootToNew[r] = newCount;
            classOf[i] = newCount;
            ++newCount;
        }
        else {
            classOf[i] = it->second;
        }
    }

    mdfa.states.resize(newCount);
    for (int i = 0; i < newCount; ++i) {
        mdfa.states[i].id = i;
    }

    // 8. 构造最小 DFA 状态和转移（取等价类代表的转移）
    for (int i = 0; i < M; ++i) {
        int ci = classOf[i];
        if (isAccept[i]) {
            mdfa.states[ci].isAccept = true;
        }
        for (char c : mdfa.alphabet) {
            auto it = trans[i].find(c);
            if (it == trans[i].end()) 
                continue;
            int to = it->second;
            int cj = classOf[to];
            mdfa.states[ci].trans[c] = cj;
        }
    }

    // 9. 新起始状态
    int oldStartReach = old2reach[dfa.start];     // 0..R-1
    mdfa.start = classOf[oldStartReach];

    return mdfa;
}

// -------------------- 输出最简 DFA --------------------

void printMinDFA(const MinDFA& dfa, std::ostream& out) {
    out << "--------------------------------------------------\n";
    if (dfa.states.empty()) {
        out << "\nDFA (minimized): Empty DFA.\n";
        return;
    }

    out << "DFA_START:" << (dfa.start + 1) << "\n";

    out << "DFA_ACCEPT:";
    bool first = true;
    for (const auto& st : dfa.states) {
        if (st.isAccept) {
            if (!first) out << " ";
            out << (st.id + 1);
            first = false;
        }
    }
    if (first) out << " (none)";
    out << "\n";

    for (const auto& st : dfa.states) {
        for (const auto& kv : st.trans) {
            char c = kv.first;
            int to = kv.second;
            out << (st.id + 1) << "->" << (to + 1) << ":" << c << "\n";
        }
    }

    out << "# DFA Alphabet: ";
    first = true;
    for (char c : dfa.alphabet) {
        if (!first) out << ", ";
        out << c;
        first = false;
    }
    out << "\n";
}

// -------------------- 主函数 --------------------

int main(int argc, char* argv[]) {
    if (argc == 1) {
        std::cerr << "Usage: " << argv[0] << " <input_file> [output_file]\n";
        return 1;
    }

    std::string fileName = argv[1];
    std::ifstream inputFile(fileName);
    if (!inputFile) {
        std::cerr << "Error: Could not open input file: " << fileName << "\n";
        return 1;
    }

    
    std::ostream* out = &std::cout;
    std::ofstream outputFile;
    if (argc >= 3) {
        outputFile.open(argv[2]);
        if (!outputFile) {
            std::cerr << "Error: Could not open output file: " << argv[2] << "\n";
            return 1;
        }
        out = &outputFile;
    }
    
    std::string regex;


    while (true) {
        if (!std::getline(inputFile, regex)) {
			std::cerr << "\nINFO: End of input file reached.\n";
            return 0;
        }

        // 1. 正则 -> NFA
        NFAFactory factory;
        NFA nfa = factory.buildFromRegex(regex);

        // 2. 输出 NFA
        printNFA(nfa, *out);

        // 3. NFA -> DFA
        DFA dfa = nfaToDfa(nfa);

        // 4. 最小化 DFA
        MinDFA mdfa = minimizeDFA(dfa);

        // 5. 输出最简 DFA
        printMinDFA(mdfa, *out);
    }

    return 0;
}
